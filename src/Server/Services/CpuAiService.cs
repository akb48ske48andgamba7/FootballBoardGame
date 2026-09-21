using FootballBoardGame.Server.Models;

namespace FootballBoardGame.Server.Services;

/// <summary>
/// 【学習用解説: 1人対戦を支える「ルールベースAI」と「フェイルセーフ設計」】
/// 
/// 人間プレイヤーの対戦相手となる CPU (TEAM RED) の自動思考・行動を担当するサービスです。
/// 
/// ■ AIの思考アルゴリズム（戦況に応じた優先度判断）:
/// 1. 【攻撃時 (HandleAttack)】
///    - ① 最優先: 相手ゴールへの直線上シュート（可能なら即シュートを放つ）
///    - ② ドリブル前進: ゴールに近づく最適なマスへ最大2マス移動
///    - ③ 前線へのパス: オフサイドにならず、かつよりゴールに近い味方へスルーパス
///    - ④ 味方サポート移動: 余った移動枠で、後方の味方を前線へ押し上げてサポート
/// 2. 【守備時 (HandleDefense)】
///    - ① タックル優先: ボール保持者のマスへ届く選手がいれば突撃してタックルデュエル発生
///    - ② 距離詰め: 届かない選手はボール保持者とのチェビシェフ距離を縮める最善のマスへ移動
/// 
/// ■ フェイルセーフ（Fail-safe: 障害耐性）の重要性:
/// - ゲームAIが予期せぬ例外（例: 同マス移動やマス制限違反）でクラッシュすると、
///   画面が止まってゲームが進行不能（フリーズ）になってしまいます。
/// - そのため、全体を try-catch で保護し、万が一の異常時にも安全に `EndTurn()` を呼んで
///   必ず手番を人間のプレイヤーに返す「止まらないシステム設計」を行っています。
/// </summary>
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
        try
        {
            var state = _gameEngine.GetCurrentState();

            // 1. 初期配置フェーズの処理
            if (state.Phase == GamePhase.SetupFirstHalfB || state.Phase == GamePhase.SetupSecondHalfB)
            {
                var randomPreset = FormationPreset.All[Random.Shared.Next(FormationPreset.All.Count)];
                state = _gameEngine.ApplyFormationPreset(cpuTeam, randomPreset.Id);
                var placements = state.Pieces
                    .Where(p => p.Team == cpuTeam)
                    .Select(p => new PiecePlacementDto(p.Id, p.Position.Row, p.Position.Col, p.Ability))
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
        catch (Exception ex)
        {
            // CPUの思考・移動中に例外が発生してもゲームが停止しないようフェイルセーフ
            var state = _gameEngine.GetCurrentState();
            state.MatchLogs.Add($"[CPU] ⚠️ CPUの行動中に軽微なエラーが発生したため、安全にターンを交代しました: {ex.Message}");
            if (state.ActiveTeam == cpuTeam && state.PendingDuel == null)
            {
                try
                {
                    state = _gameEngine.EndTurn();
                }
                catch
                {
                    // 万一EndTurnでも失敗した場合は現在の状態を返す
                }
            }
            return state;
        }
    }

    private GameState HandleAttack(Piece ballHolder, TeamType cpuTeam, GameState state)
    {
        Position targetGoal = Position.GetTargetGoal(cpuTeam, state.Half);

        // 1. シュート判定 (6マス以内かつ直線)
        if (state.CurrentTurnAction.CanPassOrShot &&
            ballHolder.Position.IsStraightLineTo(targetGoal) &&
            ballHolder.Position.ChebyshevDistance(targetGoal) <= 6)
        {
            state = _gameEngine.PassOrShot(targetGoal);
            if (state.PendingDuel != null || state.ActiveTeam != cpuTeam) return state;
        }

        // 2. ドリブル移動 (最大2マス)
        if (state.CurrentTurnAction.CanPieceMove(ballHolder))
        {
            var bestDribbleMove = FindBestMoveToward(ballHolder.Position, targetGoal, state);
            if (bestDribbleMove != null && ballHolder.Position.ChebyshevDistance(bestDribbleMove) >= 1 && ballHolder.Position.ChebyshevDistance(bestDribbleMove) <= 2)
            {
                state = _gameEngine.MovePiece(ballHolder.Id, bestDribbleMove);
                if (state.PendingDuel != null || state.ActiveTeam != cpuTeam) return state;
            }
        }

        // 3. パス判定 (パス可能回数が残っており、6マス以内の味方へ)
        if (state.CurrentTurnAction.CanPassOrShot)
        {
            var currentBallHolder = state.Pieces.FirstOrDefault(p => p.Id == state.Ball.HolderPieceId);
            if (currentBallHolder != null && currentBallHolder.Team == cpuTeam)
            {
                var candidates = state.Pieces
                    .Where(p => p.Team == cpuTeam && p.Id != currentBallHolder.Id && currentBallHolder.Position.IsStraightLineTo(p.Position))
                    .Where(p => currentBallHolder.Position.ChebyshevDistance(p.Position) <= 6)
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
                .Where(p => p.Position.ChebyshevDistance(targetGoal) > 1)
                .OrderBy(p => p.Position.ChebyshevDistance(targetGoal))
                .FirstOrDefault();

            if (otherPlayer == null) break;

            var bestMove = FindBestMoveToward(otherPlayer.Position, targetGoal, state);
            if (bestMove != null && otherPlayer.Position.ChebyshevDistance(bestMove) >= 1 && otherPlayer.Position.ChebyshevDistance(bestMove) <= 2)
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
        var enemyHolder = state.Pieces.FirstOrDefault(p => p.Id == state.Ball.HolderPieceId);
        bool isEnemyGkHolding = enemyHolder != null && enemyHolder.IsGoalkeeper;

        // タックル優先、届かなければ距離を詰める
        while (state.CurrentTurnAction.StandardMovesRemaining > 0)
        {
            var defender = state.Pieces
                .Where(p => p.Team == cpuTeam && !p.IsGoalkeeper && state.CurrentTurnAction.CanPieceMove(p))
                .Where(p => p.Position.ChebyshevDistance(targetPos) > 0) // すでにボールと同マスの選手は除外
                .OrderBy(p => p.Position.ChebyshevDistance(targetPos))
                .FirstOrDefault();

            if (defender == null) break;

            int dist = defender.Position.ChebyshevDistance(targetPos);
            // 相手GKがボール保持時、または今ターンすでにタックル済みの選手はタックル不可（距離を詰めるだけ）
            bool canTackle = !isEnemyGkHolding && state.CurrentTurnAction.CanPieceTackle(defender.Id);

            if (dist >= 1 && dist <= 2 && canTackle)
            {
                state = _gameEngine.MovePiece(defender.Id, targetPos);
                if (state.PendingDuel != null) return state;
            }
            else
            {
                var bestMove = FindBestMoveToward(defender.Position, targetPos, state);
                if (bestMove != null && defender.Position.ChebyshevDistance(bestMove) >= 1 && defender.Position.ChebyshevDistance(bestMove) <= 2)
                {
                    // GK保護またはタックル済みの場合は相手マス自体を避ける
                    if (!canTackle && bestMove.Row == targetPos.Row && bestMove.Col == targetPos.Col)
                    {
                        break;
                    }
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
