using FootballBoardGame.Server.Models;

namespace FootballBoardGame.Server.Services;

/// <summary>
/// 【学習用解説: ゲームロジックの心臓部「ドメインサービス (Domain Service)」】
/// 
/// サッカーボードゲームのルール判定、コマの移動、パス・シュート、サイコロ勝負、得点管理など、
/// ゲームにおけるすべての「ビジネスロジック（ルール）」を集中して実行する中心クラスです。
/// 
/// ■ 設計の重要ポイント:
/// 1. コントローラー (GameController) との責務分離:
///    - コントローラーは「HTTPリクエストを受け取って返すだけ」の薄い層にします。
///    - ルールの計算や状態変更はすべてこの `GameEngineService` に任せることで、
///      単体テスト (`dotnet test`) がWebサーバーを起動せず瞬時に実行できるようになります。
/// 
/// 2. 依存性の注入 (Dependency Injection: DI):
///    - サイコロ機能 (`IDiceService`) やオフサイド判定 (`IOffsideRuleService`) を
///      コンストラクタ経由で受け取ることで、コードの結合度を下げ、テストしやすい設計にしています。
/// 
/// 3. LINQ (Language Integrated Query) の活用:
///    - `Pieces.FirstOrDefault(p => ...)` や `Where(...)` など、C#の強力なデータ問い合わせ機能を
///      駆使して、ピッチ上の選手やボールの位置関係を直感的に取得・操作しています。
/// </summary>
public class GameEngineService : IGameEngineService
{
    private readonly IDiceService _diceService;
    private readonly IOffsideRuleService _offsideService;
    private GameState _state = new();

    public GameEngineService(IDiceService diceService, IOffsideRuleService offsideService)
    {
        _diceService = diceService;
        _offsideService = offsideService;
        ResetGame();
    }

    public GameState GetCurrentState()
    {
        int? offsideCol = _offsideService.GetOffsideLineCol(_state, _state.ActiveTeam);
        if (offsideCol.HasValue)
        {
            _state.OffsideWarning = $"Offside Line: Col {offsideCol.Value}";
        }
        else
        {
            _state.OffsideWarning = null;
        }

        return _state;
    }

    public GameState SetGameMode(GameMode mode)
    {
        _state.Mode = mode;
        _state.MatchLogs.Add($"対戦モードを変更しました: {(mode == GameMode.PvC ? "vs CPU対戦" : "2名対戦")}");
        return GetCurrentState();
    }

    public GameState ResetGame()
    {
        _state = new GameState
        {
            GameId = Guid.NewGuid(),
            Phase = GamePhase.SetupFirstHalfA,
            Mode = _state.Mode,
            ActiveTeam = TeamType.TeamA,
            Turn = 1,
            IsAdditionalTime = false,
            AdditionalTimeTotal = 0,
            AdditionalTimeTurnsElapsed = 0,
            ScoreTeamA = 0,
            ScoreTeamB = 0,
            Pieces = new List<Piece>(),
            Ball = new Ball { Position = new Position(4, 6), HolderPieceId = null },
            CurrentTurnAction = new TurnActionState(),
            PendingDuel = null,
            MatchLogs = new List<string> { "キックオフ準備: チームAの配置を開始してください。" }
        };

        _state.Pieces.AddRange(Piece.CreateDefaultTeam(TeamType.TeamA, 1));
        _state.Pieces.AddRange(Piece.CreateDefaultTeam(TeamType.TeamB, 1));

        ApplyDefaultFormation(TeamType.TeamA);
        ApplyDefaultFormation(TeamType.TeamB);

        return GetCurrentState();
    }

    public GameState ApplyDefaultFormation(TeamType team)
    {
        return ApplyFormationPreset(team, "4-3-3");
    }

    public GameState ApplyFormationPreset(TeamType team, string formationId)
    {
        var preset = FormationPreset.All.FirstOrDefault(p => p.Id == formationId)
            ?? FormationPreset.All.First(p => p.Id == "4-3-3");

        bool isTeamA = team == TeamType.TeamA;
        int half = _state.Half;
        bool isLeftHalf = (half == 1 && isTeamA) || (half == 2 && !isTeamA);

        var teamPieces = _state.Pieces.Where(p => p.Team == team).OrderBy(p => p.Number).ToList();

        foreach (var pos in preset.Positions)
        {
            var piece = teamPieces.FirstOrDefault(p => p.Number == pos.Number);
            if (piece != null)
            {
                int actualCol = isLeftHalf ? pos.RelativeCol : (13 - pos.RelativeCol);
                piece.Position = new Position(pos.Row, actualCol);
                piece.Ability = pos.DefaultAbility;
                piece.Name = $"{pos.PositionName} {piece.Number}{(piece.Ability == 3 ? " ★" : "")}";
            }
        }

        // ボールはキックオフ側のエース(★3)が保持
        if (team == _state.ActiveTeam)
        {
            var ace = teamPieces.FirstOrDefault(p => p.Ability == 3) ?? teamPieces.FirstOrDefault(p => p.Number == 10);
            if (ace != null)
            {
                _state.Ball.Position = ace.Position;
                _state.Ball.HolderPieceId = ace.Id;
            }
        }

        _state.MatchLogs.Add($"[{team}] フォーメーション「{preset.Name}」を適用しました。");
        return GetCurrentState();
    }

    public GameState SetupTeam(TeamType team, List<PiecePlacementDto> placements)
    {
        if (placements.Count != 11)
        {
            throw new ArgumentException("11名全員の配置を指定してください。");
        }

        int half = _state.Half;
        bool isLeftHalf = (half == 1 && team == TeamType.TeamA) || (half == 2 && team == TeamType.TeamB);
        int minCol = isLeftHalf ? 1 : 7;
        int maxCol = isLeftHalf ? 6 : 12;

        var teamPieces = _state.Pieces.Where(p => p.Team == team).ToList();

        foreach (var placement in placements)
        {
            if (placement.Row < 1 || placement.Row > 7 || placement.Col < minCol || placement.Col > maxCol)
            {
                throw new ArgumentException($"自陣 (縦 Row 1〜7, 横 Col {minCol}〜{maxCol}) の範囲内に配置してください。");
            }

            var piece = teamPieces.FirstOrDefault(p => p.Id == placement.PieceId);
            if (piece != null)
            {
                piece.Position = new Position(placement.Row, placement.Col);
                if (placement.Ability.HasValue)
                {
                    piece.Ability = placement.Ability.Value;
                }
            }
        }

        // 能力配分バリデーション: 全11名で能力3が1名、能力2は最大4名まで、残りは能力1
        int count3 = teamPieces.Count(p => p.Ability == 3);
        int count2 = teamPieces.Count(p => p.Ability == 2);
        int count1 = teamPieces.Count(p => p.Ability == 1);

        if (count3 != 1 || count2 > 4 || (count3 + count2 + count1) != 11)
        {
            throw new ArgumentException($"能力値の配分が正しくありません。(★3が1名、★2は4名まで、残りは★1で合計11名必要です。現在: ★3={count3}名, ★2={count2}名, ★1={count1}名)");
        }

        // GKが1名いることの検証
        if (teamPieces.Count(p => p.IsGoalkeeper) != 1)
        {
            throw new ArgumentException("ゴールキーパー(GK)は必ず1名必要です。");
        }

        if (_state.Phase == GamePhase.SetupFirstHalfA)
        {
            _state.Phase = GamePhase.SetupFirstHalfB;
            _state.MatchLogs.Add("チームAの配置が完了しました。チームBの配置を開始してください。");
        }
        else if (_state.Phase == GamePhase.SetupFirstHalfB)
        {
            _state.Phase = GamePhase.FirstHalf;
            _state.ActiveTeam = TeamType.TeamA;
            SaveKickoffPositions();
            StartNewTurn();
            _state.MatchLogs.Add("両チーム配置完了！前半キックオフ！");
        }
        else if (_state.Phase == GamePhase.SetupSecondHalfA)
        {
            _state.Phase = GamePhase.SetupSecondHalfB;
            _state.MatchLogs.Add("後半: チームAの配置完了。チームBの配置を開始してください。");
        }
        else if (_state.Phase == GamePhase.SetupSecondHalfB)
        {
            _state.Phase = GamePhase.SecondHalf;
            _state.ActiveTeam = TeamType.TeamB;
            SaveKickoffPositions();
            StartNewTurn();
            _state.MatchLogs.Add("後半キックオフ！");
        }

        return GetCurrentState();
    }

    public GameState MovePiece(string pieceId, Position targetPosition)
    {
        EnsurePlayingPhase();
        if (_state.PendingDuel != null)
        {
            throw new InvalidOperationException("サイコロ勝負（デュエル）が保留中です。先に解決してください。");
        }

        var piece = _state.Pieces.FirstOrDefault(p => p.Id == pieceId)
            ?? throw new ArgumentException("指定されたコマが見つかりません。");

        if (piece.Team != _state.ActiveTeam)
        {
            throw new InvalidOperationException("現在の手番チームのコマのみ移動できます。");
        }

        if (!targetPosition.IsInsidePitch)
        {
            throw new InvalidOperationException("ピッチ外へは移動できません。");
        }

        int dist = piece.Position.ChebyshevDistance(targetPosition);
        if (dist < 1 || dist > 2)
        {
            throw new InvalidOperationException("移動は1〜2マスまで可能です。");
        }

        if (!_state.CurrentTurnAction.CanPieceMove(piece))
        {
            throw new InvalidOperationException("今ターンのこのコマの移動可能回数は残っていません。");
        }

        // タックル対象（移動先に相手ボール保持者がいるか）の事前バリデーション
        var opposingTeam = _state.ActiveTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        var enemyBallHolder = _state.Pieces.FirstOrDefault(p =>
            p.Team == opposingTeam &&
            p.Position.Row == targetPosition.Row &&
            p.Position.Col == targetPosition.Col &&
            _state.Ball.HolderPieceId == p.Id);

        if (enemyBallHolder != null)
        {
            // 要件: ゴールキーパーがボールを保持しているときはタックル不可
            if (enemyBallHolder.IsGoalkeeper)
            {
                throw new InvalidOperationException("相手ゴールキーパーがボールを保持している間はタックルできません（GK保護ルール）。");
            }

            // 要件: タックルに行けるのは1選手1ターンに1度
            if (!_state.CurrentTurnAction.CanPieceTackle(piece.Id))
            {
                throw new InvalidOperationException($"{piece.Name} は今ターンすでにタックルを試みたため、再度タックルに行くことはできません（1ターン1回制限）。");
            }
        }

        bool isHoldingBall = _state.Ball.HolderPieceId == piece.Id;

        piece.Position = targetPosition;
        _state.CurrentTurnAction.RecordMove(piece);

        _state.MatchLogs.Add($"[{_state.ActiveTeam}] {piece.Name} が ({targetPosition.Row}, {targetPosition.Col}) へ移動しました。");

        if (isHoldingBall)
        {
            _state.Ball.Position = targetPosition;
        }
        else
        {
            if (!_state.Ball.IsHeld && _state.Ball.Position.Row == targetPosition.Row && _state.Ball.Position.Col == targetPosition.Col)
            {
                _state.Ball.HolderPieceId = piece.Id;
                _state.MatchLogs.Add($"[{_state.ActiveTeam}] {piece.Name} がルーズボールをキープしました！");
            }
        }

        UpdateGkPrivilege();

        // タックル判定
        if (enemyBallHolder != null)
        {
            _state.CurrentTurnAction.RecordTackle(piece.Id);

            var attackers = _state.Pieces
                .Where(p => p.Team == _state.ActiveTeam && p.Position.Row == targetPosition.Row && p.Position.Col == targetPosition.Col)
                .Select(p => new DuelParticipant { PieceId = p.Id, Name = p.Name, Number = p.Number, Ability = p.Ability, IsGoalkeeper = p.IsGoalkeeper })
                .ToList();

            var defenders = _state.Pieces
                .Where(p => p.Team == opposingTeam && p.Position.Row == targetPosition.Row && p.Position.Col == targetPosition.Col)
                .Select(p => new DuelParticipant { PieceId = p.Id, Name = p.Name, Number = p.Number, Ability = p.Ability, IsGoalkeeper = p.IsGoalkeeper })
                .ToList();

            _state.PendingDuel = new DuelContext
            {
                Type = DuelType.Tackle,
                DuelPosition = targetPosition,
                AttackingTeam = _state.ActiveTeam,
                DefendingTeam = opposingTeam,
                Attackers = attackers,
                Defenders = defenders,
                Message = $"タックル発生！ [{_state.ActiveTeam}] がボール奪取を狙ってサイコロ勝負！"
            };

            _state.MatchLogs.Add(_state.PendingDuel.Message);
        }

        return GetCurrentState();
    }

    public GameState PassOrShot(Position targetPosition)
    {
        EnsurePlayingPhase();
        if (_state.PendingDuel != null)
        {
            throw new InvalidOperationException("サイコロ勝負（デュエル）が保留中です。");
        }

        // 1ターンに最大2回までパス/シュート可能
        if (!_state.CurrentTurnAction.CanPassOrShot)
        {
            throw new InvalidOperationException("今ターンのパス/シュート可能回数（2回）を使い切りました。");
        }

        var ballHolder = _state.Pieces.FirstOrDefault(p => p.Id == _state.Ball.HolderPieceId);
        if (ballHolder == null || ballHolder.Team != _state.ActiveTeam)
        {
            throw new InvalidOperationException("ボールを保持している選手のみがパスまたはシュートを出せます。");
        }

        Position startPos = ballHolder.Position;
        Position targetGoal = Position.GetTargetGoal(_state.ActiveTeam, _state.Half);

        bool isShot = (targetPosition.Row == targetGoal.Row && targetPosition.Col == targetGoal.Col);

        if (!isShot && !targetPosition.IsInsidePitch)
        {
            throw new InvalidOperationException("パス先はピッチ内でなければなりません。");
        }

        if (startPos.Row == targetPosition.Row && startPos.Col == targetPosition.Col)
        {
            throw new InvalidOperationException("現在と同じマスにはパスできません。");
        }

        if (!startPos.IsStraightLineTo(targetPosition))
        {
            throw new InvalidOperationException("パス・シュートは縦・横・斜めの直線でなければなりません。");
        }

        // 要件: パスおよびシュートの最大飛距離は前後左右斜め6マス以内
        int distance = startPos.ChebyshevDistance(targetPosition);
        if (distance > 6)
        {
            throw new InvalidOperationException($"パス・シュートの最大距離は6マス以内です（指定距離: {distance}マス）。");
        }

        _state.CurrentTurnAction.RecordPassOrShot();

        var opposingTeam = _state.ActiveTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        var intermediatePath = startPos.GetStraightPathTo(targetPosition);

        // 守備側GKを取得
        var defendingGk = _state.Pieces.FirstOrDefault(p => p.Team == opposingTeam && p.IsGoalkeeper);
        // GKがペナルティエリア内にいるか（センターと上下のインサイド Row 3〜5 かつ ゴール側2列 Col 1〜2 または Col 11〜12）
        bool isGkInPenaltyArea = defendingGk != null && defendingGk.Position.IsInPenaltyArea(opposingTeam, _state.Half);

        // ==========================================
        // A. シュート時の判定ロジック
        // ==========================================
        if (isShot)
        {
            // シュートコース上（始点からゴール直前まで）にいる相手選手を調査
            var courseOpponents = intermediatePath
                .SelectMany(pos => _state.Pieces.Where(p => p.Team == opposingTeam && p.Position.Row == pos.Row && p.Position.Col == pos.Col))
                .Distinct()
                .ToList();

            // GK以外の相手フィールドプレーヤー（コース上のディフェンダー）
            var courseFieldDefenders = courseOpponents.Where(p => !p.IsGoalkeeper).ToList();
            bool isGkOnCourse = courseOpponents.Any(p => p.IsGoalkeeper);

            // 要件: ペナルティエリア内にGKがいる場合、またはGKがシュートコース上にいる場合、
            // シュートコースにいたディフェンダーの数だけゴールキーパーの能力を+1加算！
            if ((isGkInPenaltyArea || isGkOnCourse) && defendingGk != null)
            {
                CreateShotDuelWithGk(ballHolder, defendingGk, courseFieldDefenders.Count, targetGoal);
                return GetCurrentState();
            }

            // GKがPA外でコース上にもいないが、コース上に相手ディフェンダーがいる場合は手前のDFがシュートブロック
            if (courseFieldDefenders.Any())
            {
                var firstBlockPos = intermediatePath.First(pos => courseFieldDefenders.Any(d => d.Position.Row == pos.Row && d.Position.Col == pos.Col));
                var defendersAtStep = courseFieldDefenders.Where(d => d.Position.Row == firstBlockPos.Row && d.Position.Col == firstBlockPos.Col).ToList();
                CreateFieldBlockDuel(ballHolder, defendersAtStep, firstBlockPos, targetGoal);
                return GetCurrentState();
            }

            // 経路上に敵がおらず、GKもPA内にいなければ無人のゴールへGOAL!
            RegisterGoal(_state.ActiveTeam);
            return GetCurrentState();
        }

        // ==========================================
        // B. 通常パス時の判定ロジック
        // ==========================================
        // 1. パス経路上の敵ディフェンダーによるインターセプト
        foreach (var step in intermediatePath)
        {
            var defendersAtStep = _state.Pieces.Where(p => p.Team == opposingTeam && p.Position.Row == step.Row && p.Position.Col == step.Col).ToList();
            if (defendersAtStep.Any())
            {
                CreateInterceptDuel(ballHolder, defendersAtStep, step, targetPosition);
                return GetCurrentState();
            }
        }

        // 2. パス到達先マスに相手がいる場合のインターセプト
        var defendersAtTarget = _state.Pieces.Where(p => p.Team == opposingTeam && p.Position.Row == targetPosition.Row && p.Position.Col == targetPosition.Col).ToList();
        if (defendersAtTarget.Any())
        {
            CreateInterceptDuel(ballHolder, defendersAtTarget, targetPosition, targetPosition);
            return GetCurrentState();
        }

        // 3. オフサイド判定
        var receiver = _state.Pieces.FirstOrDefault(p => p.Team == _state.ActiveTeam && p.Position.Row == targetPosition.Row && p.Position.Col == targetPosition.Col);
        if (receiver != null && _offsideService.IsOffside(_state, _state.ActiveTeam, targetPosition))
        {
            _state.MatchLogs.Add($"【オフサイド！】 [{_state.ActiveTeam}] のパス受け手がオフサイドラインを越えていました。");
            _state.Ball.HolderPieceId = null;
            _state.Ball.Position = targetPosition;
            ForceTurnEndDueToInfraction();
            return GetCurrentState();
        }

        // 4. パス成功！
        _state.Ball.Position = targetPosition;
        if (receiver != null)
        {
            _state.Ball.HolderPieceId = receiver.Id;
            _state.MatchLogs.Add($"[{_state.ActiveTeam}] {ballHolder.Name} から {receiver.Name} へのパスが通りました！(残りパス/シュート: {_state.CurrentTurnAction.RemainingPassOrShots}回)");
        }
        else
        {
            _state.Ball.HolderPieceId = null;
            _state.MatchLogs.Add($"[{_state.ActiveTeam}] スペースへのパス！ボールが ({targetPosition.Row}, {targetPosition.Col}) へ出されました。(残りパス/シュート: {_state.CurrentTurnAction.RemainingPassOrShots}回)");
        }

        UpdateGkPrivilege();
        return GetCurrentState();
    }

    /// <summary>
    /// 【要件対応】GKとのシュート阻止勝負を生成。
    /// 手を使った守備ボーナス(+1)に加え、シュートコース上にディフェンダーがいた場合、その人数分GKの能力を+1加算します。
    /// </summary>
    private void CreateShotDuelWithGk(
        Piece attacker,
        Piece defendingGk,
        int courseDefenderCount,
        Position targetGoal)
    {
        var opposingTeam = _state.ActiveTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        int gkBonus = 1 + courseDefenderCount; // 手を使った守備+1 ＋ コース上のDF人数分+N

        var defenderParticipant = new DuelParticipant
        {
            PieceId = defendingGk.Id,
            Name = defendingGk.Name,
            Number = defendingGk.Number,
            Ability = defendingGk.Ability,
            AbilityBonus = gkBonus,
            IsGoalkeeper = true
        };

        string bonusDesc = courseDefenderCount > 0
            ? $"（🧤手守備+1 ＆ 🛡️シュートコース上DF{courseDefenderCount}名によるコース限定/壁補正+{courseDefenderCount}）"
            : "（🧤手守備+1）";
        string message = $"【シュート阻止！】 {attacker.Name} のシュートにGK {defendingGk.Name} がセービング{bonusDesc}！";

        _state.PendingDuel = new DuelContext
        {
            Type = DuelType.Shot,
            DuelPosition = defendingGk.Position,
            PassTargetPosition = targetGoal,
            AttackingTeam = _state.ActiveTeam,
            DefendingTeam = opposingTeam,
            Attackers = new List<DuelParticipant>
            {
                new() { PieceId = attacker.Id, Name = attacker.Name, Number = attacker.Number, Ability = attacker.Ability, AbilityBonus = 0, IsGoalkeeper = attacker.IsGoalkeeper }
            },
            Defenders = new List<DuelParticipant> { defenderParticipant },
            Message = message
        };

        _state.MatchLogs.Add(_state.PendingDuel.Message);
    }

    /// <summary>
    /// GK不在またはPA外の場合の、フィールドプレーヤーによるシュートブロック勝負
    /// </summary>
    private void CreateFieldBlockDuel(
        Piece attacker,
        List<Piece> defenders,
        Position blockPos,
        Position targetGoal)
    {
        var opposingTeam = _state.ActiveTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        var defenderParticipants = defenders.Select(p => new DuelParticipant
        {
            PieceId = p.Id,
            Name = p.Name,
            Number = p.Number,
            Ability = p.Ability,
            AbilityBonus = 0,
            IsGoalkeeper = false
        }).ToList();

        string message = $"【シュート阻止！】 {attacker.Name} のシュートに対してディフェンスが体を張ってブロック！";

        _state.PendingDuel = new DuelContext
        {
            Type = DuelType.Shot,
            DuelPosition = blockPos,
            PassTargetPosition = targetGoal,
            AttackingTeam = _state.ActiveTeam,
            DefendingTeam = opposingTeam,
            Attackers = new List<DuelParticipant>
            {
                new() { PieceId = attacker.Id, Name = attacker.Name, Number = attacker.Number, Ability = attacker.Ability, AbilityBonus = 0, IsGoalkeeper = attacker.IsGoalkeeper }
            },
            Defenders = defenderParticipants,
            Message = message
        };

        _state.MatchLogs.Add(_state.PendingDuel.Message);
    }

    /// <summary>
    /// パスコースまたは到着マスでのインターセプト勝負
    /// </summary>
    private void CreateInterceptDuel(
        Piece attacker,
        List<Piece> defenders,
        Position duelPos,
        Position targetPos)
    {
        var opposingTeam = _state.ActiveTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        var defenderParticipants = defenders.Select(p => new DuelParticipant
        {
            PieceId = p.Id,
            Name = p.Name,
            Number = p.Number,
            Ability = p.Ability,
            AbilityBonus = 0,
            IsGoalkeeper = p.IsGoalkeeper
        }).ToList();

        string message = $"【インターセプト発生！】 パスコースに相手選手が立ちふさがりました！";

        _state.PendingDuel = new DuelContext
        {
            Type = DuelType.Intercept,
            DuelPosition = duelPos,
            PassTargetPosition = targetPos,
            AttackingTeam = _state.ActiveTeam,
            DefendingTeam = opposingTeam,
            Attackers = new List<DuelParticipant>
            {
                new() { PieceId = attacker.Id, Name = attacker.Name, Number = attacker.Number, Ability = attacker.Ability, AbilityBonus = 0, IsGoalkeeper = attacker.IsGoalkeeper }
            },
            Defenders = defenderParticipants,
            Message = message
        };

        _state.MatchLogs.Add(_state.PendingDuel.Message);
    }

    public GameState ResolveDuel()
    {
        if (_state.PendingDuel == null)
        {
            throw new InvalidOperationException("現在サイコロ勝負は発生していません。");
        }

        var duel = _state.PendingDuel;
        duel.AttackerDice = _diceService.Roll();
        duel.DefenderDice = _diceService.Roll();

        bool attackerWins = duel.AttackerTotal > duel.DefenderTotal;
        duel.Winner = attackerWins ? duel.AttackingTeam : duel.DefendingTeam;

        _state.MatchLogs.Add($"[サイコロ判定] 攻撃側: 能力{duel.AttackerAbilitySum} + 出目{duel.AttackerDice} = {duel.AttackerTotal} VS 守備側: 能力{duel.DefenderAbilitySum} + 出目{duel.DefenderDice} = {duel.DefenderTotal}");

        if (duel.Type == DuelType.Tackle)
        {
            if (attackerWins)
            {
                var mainAttacker = _state.Pieces.First(p => p.Id == duel.Attackers[0].PieceId);
                _state.Ball.HolderPieceId = mainAttacker.Id;
                _state.Ball.Position = duel.DuelPosition;
                _state.MatchLogs.Add($"【タックル成功！】 [{duel.AttackingTeam}] {mainAttacker.Name} がボールを奪いました！");
            }
            else
            {
                _state.MatchLogs.Add($"【タックル失敗！】 [{duel.DefendingTeam}] が体を張ってボールをキープ！");
            }
        }
        else if (duel.Type == DuelType.Intercept)
        {
            if (attackerWins)
            {
                _state.MatchLogs.Add($"【パス通過！】 パスがディフェンスを越えて通りました！");
                if (duel.PassTargetPosition != null)
                {
                    _state.Ball.Position = duel.PassTargetPosition;
                    var receiver = _state.Pieces.FirstOrDefault(p => p.Team == duel.AttackingTeam && p.Position.Row == duel.PassTargetPosition.Row && p.Position.Col == duel.PassTargetPosition.Col);
                    _state.Ball.HolderPieceId = receiver?.Id;
                }
            }
            else
            {
                var interceptor = _state.Pieces.First(p => p.Id == duel.Defenders[0].PieceId);
                _state.Ball.HolderPieceId = interceptor.Id;
                _state.Ball.Position = duel.DuelPosition;
                _state.MatchLogs.Add($"【パスカット！】 [{duel.DefendingTeam}] {interceptor.Name} がインターセプトしました！");
            }
        }
        else if (duel.Type == DuelType.Shot)
        {
            if (attackerWins)
            {
                RegisterGoal(duel.AttackingTeam);
            }
            else
            {
                var saver = _state.Pieces.First(p => p.Id == duel.Defenders[0].PieceId);
                _state.Ball.HolderPieceId = saver.Id;
                _state.Ball.Position = duel.DuelPosition;
                _state.MatchLogs.Add($"【ファインセーブ！】 [{duel.DefendingTeam}] {saver.Name} がゴールを死守しました！");
            }
        }

        UpdateGkPrivilege();
        _state.LastResolvedDuel = duel;
        _state.PendingDuel = null;
        return GetCurrentState();
    }

    private void SaveKickoffPositions()
    {
        foreach (var piece in _state.Pieces)
        {
            piece.KickoffPosition = new Position(piece.Position.Row, piece.Position.Col);
        }
    }

    private void RegisterGoal(TeamType scoringTeam)
    {
        if (scoringTeam == TeamType.TeamA)
        {
            _state.ScoreTeamA++;
        }
        else
        {
            _state.ScoreTeamB++;
        }

        _state.MatchLogs.Add($"★★★★ GOOOOOAL!! ★★★★ [{scoringTeam}] 得点！ ({_state.ScoreTeamA} - {_state.ScoreTeamB})");

        var concededTeam = scoringTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        int half = _state.Half;

        // 実際のサッカー同様、全選手を自陣のキックオフ初期陣形へ復帰
        foreach (var piece in _state.Pieces)
        {
            if (piece.KickoffPosition != null)
            {
                piece.Position = new Position(piece.KickoffPosition.Row, piece.KickoffPosition.Col);
            }
            else
            {
                // KickoffPositionが未設定の場合は現在の自陣側へ安全に再配置
                bool isPieceTeamLeft = (half == 1 && piece.Team == TeamType.TeamA) || (half == 2 && piece.Team == TeamType.TeamB);
                int defaultCol = isPieceTeamLeft ? Math.Min(piece.Position.Col, 6) : Math.Max(piece.Position.Col, 7);
                piece.Position = new Position(piece.Position.Row, defaultCol);
            }
        }

        // 失点側チーム（キックオフを行うチーム）のセンターサークルキックオフ位置
        // 前半: TeamA自陣は左(Col 1〜6), TeamB自陣は右(Col 7〜12) -> TeamAが失点した場合はCol 6, TeamBが失点した場合はCol 7
        // 後半: 陣地交代するため逆
        bool isConcededOnLeft = (half == 1 && concededTeam == TeamType.TeamA) || (half == 2 && concededTeam == TeamType.TeamB);
        int kickoffCol = isConcededOnLeft ? 6 : 7;
        var kickoffPos = new Position(4, kickoffCol);

        _state.Ball.Position = kickoffPos;

        var kickOffPlayer = _state.Pieces.FirstOrDefault(p => p.Team == concededTeam && p.Number == 10)
            ?? _state.Pieces.FirstOrDefault(p => p.Team == concededTeam && !p.IsGoalkeeper);

        if (kickOffPlayer != null)
        {
            kickOffPlayer.Position = kickoffPos;
            _state.Ball.HolderPieceId = kickOffPlayer.Id;
        }
        else
        {
            _state.Ball.HolderPieceId = null;
        }

        _state.ActiveTeam = concededTeam;
        StartNewTurn();
        _state.MatchLogs.Add($"[{concededTeam}] 両チームが自陣のキックオフ陣形に戻り、センターサークルから試合再開！");
    }

    private void ForceTurnEndDueToInfraction()
    {
        _state.ActiveTeam = _state.ActiveTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        StartNewTurn();
    }

    public GameState EndTurn()
    {
        EnsurePlayingPhase();
        if (_state.PendingDuel != null)
        {
            throw new InvalidOperationException("サイコロ勝負（デュエル）が未解決です。");
        }

        if (_state.ActiveTeam == TeamType.TeamA)
        {
            _state.ActiveTeam = TeamType.TeamB;
            StartNewTurn();
            _state.MatchLogs.Add($"手番交代: [{_state.ActiveTeam}] のターンです。");
            return GetCurrentState();
        }

        _state.ActiveTeam = TeamType.TeamA;

        if (!_state.IsAdditionalTime)
        {
            if (_state.Turn < 45)
            {
                _state.Turn++;
                StartNewTurn();
                _state.MatchLogs.Add($"ターン {_state.Turn} / 45 開始。");
            }
            else
            {
                var (d1, d2, totalAt) = _diceService.RollTwo();
                _state.IsAdditionalTime = true;
                _state.AdditionalTimeTotal = totalAt;
                _state.AdditionalTimeTurnsElapsed = 0;
                _state.MatchLogs.Add($"【アディショナルタイム突入！】 サイコロ出目 ({d1} + {d2}) ＝ ＋{totalAt} ターンの追加！");
                StartNewTurn();
            }
        }
        else
        {
            _state.AdditionalTimeTurnsElapsed++;
            if (_state.AdditionalTimeTurnsElapsed < _state.AdditionalTimeTotal)
            {
                _state.MatchLogs.Add($"アディショナルタイム進行中 ({_state.AdditionalTimeTurnsElapsed}/{_state.AdditionalTimeTotal})");
                StartNewTurn();
            }
            else
            {
                if (_state.Phase == GamePhase.FirstHalf)
                {
                    _state.Phase = GamePhase.HalfTime;
                    _state.Turn = 1;
                    _state.IsAdditionalTime = false;
                    _state.AdditionalTimeTotal = 0;
                    _state.AdditionalTimeTurnsElapsed = 0;
                    _state.MatchLogs.Add($"【前半終了！】 ハーフタイムです。陣地を交代して後半の配置を行ってください。");
                    ApplyDefaultFormation(TeamType.TeamA);
                    ApplyDefaultFormation(TeamType.TeamB);
                    _state.Phase = GamePhase.SetupSecondHalfA;
                }
                else if (_state.Phase == GamePhase.SecondHalf)
                {
                    _state.Phase = GamePhase.GameOver;
                    string result = _state.ScoreTeamA > _state.ScoreTeamB ? "チームAの勝利！" :
                                    _state.ScoreTeamB > _state.ScoreTeamA ? "チームBの勝利！" : "引き分け！";
                    _state.MatchLogs.Add($"【試合終了！】 最終スコア: {_state.ScoreTeamA} - {_state.ScoreTeamB} ({result})");
                }
            }
        }

        return GetCurrentState();
    }

    private void StartNewTurn()
    {
        _state.CurrentTurnAction = new TurnActionState();
        UpdateGkPrivilege();
    }

    private void UpdateGkPrivilege()
    {
        var currentTeamGk = _state.Pieces.FirstOrDefault(p => p.Team == _state.ActiveTeam && p.IsGoalkeeper);
        if (currentTeamGk != null && _state.Ball.HolderPieceId == currentTeamGk.Id)
        {
            _state.CurrentTurnAction.GkBonusAvailable = true;
        }
    }

    private void EnsurePlayingPhase()
    {
        if (_state.Phase != GamePhase.FirstHalf && _state.Phase != GamePhase.SecondHalf)
        {
            throw new InvalidOperationException($"試合中ではありません (現在のフェーズ: {_state.Phase})。");
        }
    }
}
