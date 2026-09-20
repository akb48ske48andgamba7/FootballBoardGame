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

        // 試合中以外またはCPUの手番でない場合は何もしない
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
            // === 攻撃AI ===
            state = HandleAttack(ballHolder, cpuTeam, state);
        }
        else
        {
            // === 守備AI ===
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

        // 1. シュート判定: 直線が通っていればシュート
        if (!state.CurrentTurnAction.HasPassedOrShot && ballHolder.Position.IsStraightLineTo(targetGoal))
        {
            state = _gameEngine.PassOrShot(targetGoal);
            if (state.PendingDuel != null || state.ActiveTeam != cpuTeam) return state;
        }

        // 2. 移動（ドリブル）: ボール保持者を相手ゴール寄りに前進
        if (state.CurrentTurnAction.CanPieceMove(ballHolder))
        {
            var bestDribbleMove = FindBestMoveToward(ballHolder.Position, targetGoal, state);
            if (bestDribbleMove != null)
            {
                state = _gameEngine.MovePiece(ballHolder.Id, bestDribbleMove);
                if (state.PendingDuel != null || state.ActiveTeam != cpuTeam) return state;
            }
        }

        // 3. パス判定: 前方の味方を探す
        if (!state.CurrentTurnAction.HasPassedOrShot)
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
                    // 現在よりゴールに近い位置の味方ならパス
                    if (targetTeammate.Position.ChebyshevDistance(targetGoal) < currentBallHolder.Position.ChebyshevDistance(targetGoal))
                    {
                        state = _gameEngine.PassOrShot(targetTeammate.Position);
                        if (state.PendingDuel != null || state.ActiveTeam != cpuTeam) return state;
                    }
                }
            }
        }

        // 4. 残り移動枠があれば、別のフィールドプレイヤーを前進
        if (state.CurrentTurnAction.StandardMovesRemaining > 0)
        {
            var otherPlayer = state.Pieces
                .Where(p => p.Team == cpuTeam && !p.IsGoalkeeper && p.Id != state.Ball.HolderPieceId && state.CurrentTurnAction.CanPieceMove(p))
                .OrderBy(p => p.Position.ChebyshevDistance(targetGoal))
                .FirstOrDefault();

            if (otherPlayer != null)
            {
                var bestMove = FindBestMoveToward(otherPlayer.Position, targetGoal, state);
                if (bestMove != null)
                {
                    state = _gameEngine.MovePiece(otherPlayer.Id, bestMove);
                }
            }
        }

        return state;
    }

    private GameState HandleDefense(TeamType cpuTeam, GameState state)
    {
        var targetPos = state.Ball.Position;

        // 1. タックル可能か確認（最大2マスで相手ボール保持者マスへ行ける選手）
        var defenderPieces = state.Pieces
            .Where(p => p.Team == cpuTeam && !p.IsGoalkeeper && state.CurrentTurnAction.CanPieceMove(p))
            .OrderBy(p => p.Position.ChebyshevDistance(targetPos))
            .ToList();

        foreach (var def in defenderPieces)
        {
            if (state.CurrentTurnAction.StandardMovesRemaining == 0) break;

            if (def.Position.ChebyshevDistance(targetPos) <= 2)
            {
                // タックル！
                state = _gameEngine.MovePiece(def.Id, targetPos);
                if (state.PendingDuel != null) return state;
            }
            else
            {
                // ボールへ向かって最大前進
                var bestMove = FindBestMoveToward(def.Position, targetPos, state);
                if (bestMove != null)
                {
                    state = _gameEngine.MovePiece(def.Id, bestMove);
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

        // 目標地点に最も近いマスを選択
        return validMoves
            .OrderBy(p => p.ChebyshevDistance(target))
            .ThenBy(p => Math.Abs(p.Row - target.Row))
            .FirstOrDefault();
    }
}
