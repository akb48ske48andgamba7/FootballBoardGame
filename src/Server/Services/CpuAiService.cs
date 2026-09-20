using FootballBoardGame.Server.Models;

namespace FootballBoardGame.Server.Services;

public class CpuAiService : ICpuAiService
{
    private readonly IGameEngineService _gameEngine;
    private readonly IOffsideRuleService _offsideService;

    public CpuAiService(IGameEngineService gameEngine, IOffsideRuleService offsideService)
    {
        _gameEngine = gameEngine;
        _offsideService = offsideService;
    }

    public GameState ExecuteCpuStep(TeamType cpuTeam = TeamType.TeamB)
    {
        var state = _gameEngine.GetCurrentState();

        // 1. 初期配置フェーズの処理
        if (state.Phase == GamePhase.SetupFirstHalfB || state.Phase == GamePhase.SetupSecondHalfB)
        {
            _gameEngine.ApplyDefaultFormation(cpuTeam);
            var placements = state.Pieces
                .Where(p => p.Team == cpuTeam)
                .Select(p => new PiecePlacementDto(p.Id, p.Position.Row, p.Position.Col))
                .ToList();
            return _gameEngine.SetupTeam(cpuTeam, placements);
        }

        if ((state.Phase != GamePhase.FirstHalf && state.Phase != GamePhase.SecondHalf) ||
            state.ActiveTeam != cpuTeam)
        {
            return state;
        }

        // 2. デュエルが未解決なら解決
        if (state.PendingDuel != null)
        {
            state = _gameEngine.ResolveDuel();
            if (state.PendingDuel != null) return state;
        }

        var ballHolder = state.Pieces.FirstOrDefault(p => p.Id == state.Ball.HolderPieceId);
        bool cpuHoldsBall = ballHolder != null && ballHolder.Team == cpuTeam;

        if (cpuHoldsBall && ballHolder != null)
        {
            state = HandleAttack(ballHolder, cpuTeam, state);
        }
        else
        {
            state = HandleDefense(cpuTeam, state);
        }

        // アクション完了後、デュエルが発生していなければターン終了
        if (state.PendingDuel == null && state.ActiveTeam == cpuTeam)
        {
            state = _gameEngine.EndTurn();
        }

        return state;
    }

    private GameState HandleAttack(Piece ballHolder, TeamType cpuTeam, GameState state)
    {
        Position targetGoal = Position.GetTargetGoal(cpuTeam, state.Half);

        // 1. シュート判定
        if (state.CurrentTurnAction.CanPassOrShot && ballHolder.Position.IsStraightLineTo(targetGoal))
        {
            state = _gameEngine.PassOrShot(targetGoal);
            if (state.PendingDuel != null || state.ActiveTeam != cpuTeam) return state;
        }

        // 2. ドリブル移動 (最大2マス)
        if (state.CurrentTurnAction.CanPieceMove(ballHolder))
        {
            var bestDribbleMove = FindBestMoveToward(ballHolder.Position, targetGoal, state);
            if (bestDribbleMove != null)
            {
                state = _gameEngine.MovePiece(ballHolder.Id, bestDribbleMove);
                if (state.PendingDuel != null || state.ActiveTeam != cpuTeam) return state;
            }
        }

        // 3. パス判定 (パス可能回数が残っている場合)
        if (state.CurrentTurnAction.CanPassOrShot)
        {
            var currentBallHolder = state.Pieces.FirstOrDefault(p => p.Id == state.Ball.HolderPieceId);
            if (currentBallHolder != null && currentBallHolder.Team == cpuTeam)
            {
                var candidates = state.Pieces
                    .Where(p => p.Team == cpuTeam && p.Id != currentBallHolder.Id && currentBallHolder.Position.IsStraightLineTo(p.Position))
                    .Where(p => !_offsideService.IsOffside(state, cpuTeam, p.Position))
                    .OrderBy(p => p.Position.ChebyshevDistance(targetGoal))
                    .ToList();

                if (candidates.Any())
                {
                    var targetTeammate = candidates.First();
                    if (targetTeammate.Position.ChebyshevDistance(targetGoal) < currentBallHolder.Position.ChebyshevDistance(targetGoal))
                    {
                        state = _gameEngine.PassOrShot(targetTeammate.Position);
                        if (state.PendingDuel != null || state.ActiveTeam != cpuTeam) return state;
                    }
                }
            }
        }

        // 4. 残り移動枠（最大3枠）を使って、前線の味方をサポート移動
        while (state.CurrentTurnAction.StandardMovesRemaining > 0)
        {
            var otherPlayer = state.Pieces
                .Where(p => p.Team == cpuTeam && !p.IsGoalkeeper && p.Id != state.Ball.HolderPieceId && state.CurrentTurnAction.CanPieceMove(p))
                .OrderBy(p => p.Position.ChebyshevDistance(targetGoal))
                .FirstOrDefault();

            if (otherPlayer == null) break;

            var bestMove = FindBestMoveToward(otherPlayer.Position, targetGoal, state);
            if (bestMove != null)
            {
                state = _gameEngine.MovePiece(otherPlayer.Id, bestMove);
            }
            else
            {
                break;
            }
        }

        return state;
    }

    private GameState HandleDefense(TeamType cpuTeam, GameState state)
    {
        var targetPos = state.Ball.Position;

        // タックル優先、届かなければ距離を詰める
        while (state.CurrentTurnAction.StandardMovesRemaining > 0)
        {
            var defender = state.Pieces
                .Where(p => p.Team == cpuTeam && !p.IsGoalkeeper && state.CurrentTurnAction.CanPieceMove(p))
                .OrderBy(p => p.Position.ChebyshevDistance(targetPos))
                .FirstOrDefault();

            if (defender == null) break;

            if (defender.Position.ChebyshevDistance(targetPos) <= 2)
            {
                state = _gameEngine.MovePiece(defender.Id, targetPos);
                if (state.PendingDuel != null) return state;
            }
            else
            {
                var bestMove = FindBestMoveToward(defender.Position, targetPos, state);
                if (bestMove != null)
                {
                    state = _gameEngine.MovePiece(defender.Id, bestMove);
                }
                else
                {
                    break;
                }
            }
        }

        return state;
    }

    private Position? FindBestMoveToward(Position current, Position target, GameState state)
    {
        var validMoves = new List<Position>();

        for (int dr = -2; dr <= 2; dr++)
        {
            for (int dc = -2; dc <= 2; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                var pos = new Position(current.Row + dr, current.Col + dc);
                if (pos.IsInsidePitch && current.ChebyshevDistance(pos) <= 2)
                {
                    validMoves.Add(pos);
                }
            }
        }

        if (!validMoves.Any()) return null;

        return validMoves
            .OrderBy(p => p.ChebyshevDistance(target))
            .ThenBy(p => Math.Abs(p.Row - target.Row))
            .FirstOrDefault();
    }
}
