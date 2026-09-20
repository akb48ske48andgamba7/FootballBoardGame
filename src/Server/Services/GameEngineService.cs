using FootballBoardGame.Server.Models;

namespace FootballBoardGame.Server.Services;

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

        // 能力配分バリデーション: 全11名で能力3が1名、能力2が3名、能力1が7名
        int count3 = teamPieces.Count(p => p.Ability == 3);
        int count2 = teamPieces.Count(p => p.Ability == 2);
        int count1 = teamPieces.Count(p => p.Ability == 1);

        if (count3 != 1 || count2 != 3 || count1 != 7)
        {
            throw new ArgumentException($"能力値の配分が正しくありません。(★3が1名、★2が3名、★1が7名必要です。現在: ★3={count3}名, ★2={count2}名, ★1={count1}名)");
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
        var opposingTeam = _state.ActiveTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        var enemyBallHolder = _state.Pieces.FirstOrDefault(p => p.Team == opposingTeam && p.Position.Row == targetPosition.Row && p.Position.Col == targetPosition.Col && _state.Ball.HolderPieceId == p.Id);

        if (enemyBallHolder != null)
        {
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

        _state.CurrentTurnAction.RecordPassOrShot();

        var opposingTeam = _state.ActiveTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        var intermediatePath = startPos.GetStraightPathTo(targetPosition);

        // 守備側GKを取得
        var defendingGk = _state.Pieces.FirstOrDefault(p => p.Team == opposingTeam && p.IsGoalkeeper);
        // GKがペナルティエリア内にいるか（センターと上下のインサイド Row 3〜5 かつ ゴールに最も近い列 Col 1 または Col 12）
        bool isGkInPenaltyArea = defendingGk != null && defendingGk.Position.IsInPenaltyArea(opposingTeam, _state.Half);

        // ルート上の敵ディフェンダーチェック
        foreach (var step in intermediatePath)
        {
            var defendersAtStep = _state.Pieces.Where(p => p.Team == opposingTeam && p.Position.Row == step.Row && p.Position.Col == step.Col).ToList();
            if (defendersAtStep.Any())
            {
                CreateInterceptOrShotDuel(ballHolder, defendersAtStep, step, targetPosition, isShot, isGkInPenaltyArea, defendingGk);
                return GetCurrentState();
            }
        }

        // シュートの場合
        if (isShot)
        {
            // ゴール直前マスの守備者チェック
            int goalEntranceCol = targetGoal.Col == 0 ? 1 : 12;
            var goalEntranceDefenders = _state.Pieces
                .Where(p => p.Team == opposingTeam && p.Position.Row == targetGoal.Row && p.Position.Col == goalEntranceCol)
                .ToList();

            if (goalEntranceDefenders.Any())
            {
                CreateInterceptOrShotDuel(ballHolder, goalEntranceDefenders, new Position(targetGoal.Row, goalEntranceCol), targetGoal, true, isGkInPenaltyArea, defendingGk);
                return GetCurrentState();
            }

            // 要件: ペナルティエリア内（Row 3〜5, Col 1 or 12）にGKがいる場合、シュート直線上にいなくても必ずGKとシュート阻止勝負！
            if (isGkInPenaltyArea && defendingGk != null)
            {
                // GKがいるマスでシュート阻止勝負を発生
                var defenders = _state.Pieces
                    .Where(p => p.Team == opposingTeam && p.Position.Row == defendingGk.Position.Row && p.Position.Col == defendingGk.Position.Col)
                    .ToList();

                CreateInterceptOrShotDuel(ballHolder, defenders, defendingGk.Position, targetGoal, true, true, defendingGk);
                return GetCurrentState();
            }

            // 経路上に敵がおらず、GKもPA内にいなければ無人のゴールへGOAL!
            RegisterGoal(_state.ActiveTeam);
            return GetCurrentState();
        }

        // 通常パスの場合: 到着先マスに相手がいるか
        var defendersAtTarget = _state.Pieces.Where(p => p.Team == opposingTeam && p.Position.Row == targetPosition.Row && p.Position.Col == targetPosition.Col).ToList();
        if (defendersAtTarget.Any())
        {
            CreateInterceptOrShotDuel(ballHolder, defendersAtTarget, targetPosition, targetPosition, false, false, null);
            return GetCurrentState();
        }

        // オフサイド判定
        var receiver = _state.Pieces.FirstOrDefault(p => p.Team == _state.ActiveTeam && p.Position.Row == targetPosition.Row && p.Position.Col == targetPosition.Col);
        if (receiver != null && _offsideService.IsOffside(_state, _state.ActiveTeam, targetPosition))
        {
            _state.MatchLogs.Add($"【オフサイド！】 [{_state.ActiveTeam}] のパス受け手がオフサイドラインを越えていました。");
            _state.Ball.HolderPieceId = null;
            _state.Ball.Position = targetPosition;
            ForceTurnEndDueToInfraction();
            return GetCurrentState();
        }

        // パス成功！
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

    private void CreateInterceptOrShotDuel(
        Piece attacker,
        List<Piece> defenders,
        Position duelPos,
        Position targetPos,
        bool isShot,
        bool isGkInPenaltyArea,
        Piece? defendingGk)
    {
        var opposingTeam = _state.ActiveTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;

        // もしシュートでGKがPA内にいて、defendersに含まれていない場合はGKも守備陣に合流
        var finalDefenders = new List<Piece>(defenders);
        if (isShot && isGkInPenaltyArea && defendingGk != null && !finalDefenders.Any(d => d.Id == defendingGk.Id))
        {
            finalDefenders.Add(defendingGk);
        }

        // 要件: ゴールに向かうシュートが打たれたときのみ、ディフェンス側GKは能力を+1する（手を使って守れるルール）
        var defenderParticipants = finalDefenders.Select(p => new DuelParticipant
        {
            PieceId = p.Id,
            Name = p.Name,
            Number = p.Number,
            Ability = p.Ability,
            // シュート時のGKには能力+1ボーナスを付与！
            AbilityBonus = (isShot && p.IsGoalkeeper) ? 1 : 0,
            IsGoalkeeper = p.IsGoalkeeper
        }).ToList();

        string message;
        if (isShot)
        {
            bool hasGk = defenderParticipants.Any(d => d.IsGoalkeeper);
            message = hasGk
                ? $"【シュート阻止！】 {attacker.Name} のシュートにGKがセービング（手を使って能力+1）！"
                : $"【シュート阻止！】 {attacker.Name} のシュートに対してディフェンスが体を張ってブロック！";
        }
        else
        {
            message = $"【インターセプト発生！】 パスコースに相手選手が立ちふさがりました！";
        }

        _state.PendingDuel = new DuelContext
        {
            Type = isShot ? DuelType.Shot : DuelType.Intercept,
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
        _state.PendingDuel = null;
        return GetCurrentState();
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
        int kickoffCol = concededTeam == TeamType.TeamA ? 6 : 7;
        _state.Ball.Position = new Position(4, kickoffCol);

        var kickOffPlayer = _state.Pieces.FirstOrDefault(p => p.Team == concededTeam && p.Number == 10);
        if (kickOffPlayer != null)
        {
            kickOffPlayer.Position = new Position(4, kickoffCol);
            _state.Ball.HolderPieceId = kickOffPlayer.Id;
        }
        else
        {
            _state.Ball.HolderPieceId = null;
        }

        _state.ActiveTeam = concededTeam;
        StartNewTurn();
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
