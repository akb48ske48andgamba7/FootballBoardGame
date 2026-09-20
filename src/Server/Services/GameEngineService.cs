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
        // リアルタイムのオフサイドライン列番号(Col)を計算して付与
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
            Mode = _state.Mode, // 前回のモードを維持
            ActiveTeam = TeamType.TeamA,
            Turn = 1,
            IsAdditionalTime = false,
            AdditionalTimeTotal = 0,
            AdditionalTimeTurnsElapsed = 0,
            ScoreTeamA = 0,
            ScoreTeamB = 0,
            Pieces = new List<Piece>(),
            Ball = new Ball { Position = new Position(4, 5), HolderPieceId = null },
            CurrentTurnAction = new TurnActionState(),
            PendingDuel = null,
            MatchLogs = new List<string> { "キックオフ準備: チームAの配置を開始してください。" }
        };

        // チームAとBのデフォルトピース（11名ずつ）を生成
        _state.Pieces.AddRange(Piece.CreateDefaultTeam(TeamType.TeamA, 1));
        _state.Pieces.AddRange(Piece.CreateDefaultTeam(TeamType.TeamB, 1));

        // デフォルトのフォーメーションをセット
        ApplyDefaultFormation(TeamType.TeamA);
        ApplyDefaultFormation(TeamType.TeamB);

        return GetCurrentState();
    }

    public GameState ApplyDefaultFormation(TeamType team)
    {
        bool isTeamA = team == TeamType.TeamA;
        int half = _state.Half;

        // 自陣のベース列（前半: TeamAは左側 Col 1〜5、TeamBは右側 Col 6〜10 / 後半は逆）
        bool isLeftHalf = (half == 1 && isTeamA) || (half == 2 && !isTeamA);

        int gkCol = isLeftHalf ? 1 : 10;
        int dfCol = isLeftHalf ? 2 : 9;
        int mfCol = isLeftHalf ? 3 : 8;
        int fwCol = isLeftHalf ? 5 : 6;

        var teamPieces = _state.Pieces.Where(p => p.Team == team).OrderBy(p => p.Number).ToList();

        foreach (var p in teamPieces)
        {
            switch (p.Number)
            {
                case 1: // GK (中央レーン Row 4)
                    p.Position = new Position(4, gkCol);
                    break;
                case 2: // DF 上
                    p.Position = new Position(2, dfCol);
                    break;
                case 3: // DF 上インサイド
                    p.Position = new Position(3, dfCol);
                    break;
                case 4: // DF 下インサイド
                    p.Position = new Position(5, dfCol);
                    break;
                case 5: // DF 下
                    p.Position = new Position(6, dfCol);
                    break;
                case 6: // MF 上
                    p.Position = new Position(2, mfCol);
                    break;
                case 7: // MF センター
                    p.Position = new Position(4, mfCol);
                    break;
                case 8: // MF 下
                    p.Position = new Position(6, mfCol);
                    break;
                case 9: // FW 上
                    p.Position = new Position(3, fwCol);
                    break;
                case 10: // FW センター (Ace ★3)
                    p.Position = new Position(4, fwCol);
                    break;
                case 11: // FW 下
                    p.Position = new Position(5, fwCol);
                    break;
            }
        }

        // ボールはキックオフ側FW（背番号10）が保持
        if (team == _state.ActiveTeam)
        {
            var ace = teamPieces.FirstOrDefault(p => p.Number == 10);
            if (ace != null)
            {
                _state.Ball.Position = ace.Position;
                _state.Ball.HolderPieceId = ace.Id;
            }
        }

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
        int minCol = isLeftHalf ? 1 : 6;
        int maxCol = isLeftHalf ? 5 : 10;

        foreach (var placement in placements)
        {
            if (placement.Row < 1 || placement.Row > 7 || placement.Col < minCol || placement.Col > maxCol)
            {
                throw new ArgumentException($"自陣 (縦 Row 1〜7, 横 Col {minCol}〜{maxCol}) の範囲内に配置してください。");
            }

            var piece = _state.Pieces.FirstOrDefault(p => p.Id == placement.PieceId && p.Team == team);
            if (piece != null)
            {
                piece.Position = new Position(placement.Row, placement.Col);
            }
        }

        // フェーズの進行
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
            _state.ActiveTeam = TeamType.TeamB; // 後半はチームBからキックオフ
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

        // 移動可能チェック (最大2マス)
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

        // 移動実行（味方や相手をすり抜けて移動可能）
        piece.Position = targetPosition;
        _state.CurrentTurnAction.RecordMove(piece);

        _state.MatchLogs.Add($"[{_state.ActiveTeam}] {piece.Name} が ({targetPosition.Row}, {targetPosition.Col}) へ移動しました。");

        // ボール保持状態の場合、ボールも追従
        if (isHoldingBall)
        {
            _state.Ball.Position = targetPosition;
        }
        else
        {
            // ボールだけが置かれているマスに移動した場合、自動的にボールを保持する
            if (!_state.Ball.IsHeld && _state.Ball.Position.Row == targetPosition.Row && _state.Ball.Position.Col == targetPosition.Col)
            {
                _state.Ball.HolderPieceId = piece.Id;
                _state.MatchLogs.Add($"[{_state.ActiveTeam}] {piece.Name} がルーズボールをキープしました！");
            }
        }

        // GK特権の判定更新: GKがボールを保持しているターンに限り追加移動可能
        UpdateGkPrivilege();

        // 発生条件1（タックル）: 相手がボールを保持しているマスに自分のコマを移動させた場合、移動直後に自動でサイコロ勝負が発生
        var opposingTeam = _state.ActiveTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        var enemyBallHolder = _state.Pieces.FirstOrDefault(p => p.Team == opposingTeam && p.Position.Row == targetPosition.Row && p.Position.Col == targetPosition.Col && _state.Ball.HolderPieceId == p.Id);

        if (enemyBallHolder != null)
        {
            // そのマスにいる自分のチーム選手全員（アタッカー側）
            var attackers = _state.Pieces
                .Where(p => p.Team == _state.ActiveTeam && p.Position.Row == targetPosition.Row && p.Position.Col == targetPosition.Col)
                .Select(p => new DuelParticipant { PieceId = p.Id, Name = p.Name, Number = p.Number, Ability = p.Ability, IsGoalkeeper = p.IsGoalkeeper })
                .ToList();

            // そのマスにいる相手選手全員（ディフェンダー側）
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

        if (_state.CurrentTurnAction.HasPassedOrShot)
        {
            throw new InvalidOperationException("今ターンはすでにパスまたはシュートを実行しました。");
        }

        var ballHolder = _state.Pieces.FirstOrDefault(p => p.Id == _state.Ball.HolderPieceId);
        if (ballHolder == null || ballHolder.Team != _state.ActiveTeam)
        {
            throw new InvalidOperationException("ボールを保持している選手のみがパスまたはシュートを出せます。");
        }

        Position startPos = ballHolder.Position;
        Position targetGoal = Position.GetTargetGoal(_state.ActiveTeam, _state.Half);

        // ゴールへ向かって放たれるパスは「シュート」として扱う
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

        _state.CurrentTurnAction.HasPassedOrShot = true;

        var opposingTeam = _state.ActiveTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;

        // 直線ルート上のマスリスト (始点と終点は除外)
        var intermediatePath = startPos.GetStraightPathTo(targetPosition);

        // ルート上に相手選手がいるかチェック（インターセプトまたはシュート阻止）
        foreach (var step in intermediatePath)
        {
            var defendersAtStep = _state.Pieces.Where(p => p.Team == opposingTeam && p.Position.Row == step.Row && p.Position.Col == step.Col).ToList();
            if (defendersAtStep.Any())
            {
                CreateInterceptOrShotDuel(ballHolder, defendersAtStep, step, targetPosition, isShot);
                return GetCurrentState();
            }
        }

        // シュートの場合、ゴール枠直前（Row 4, Col 1 または Col 10）に相手GK/DFがいればブロック勝負
        if (isShot)
        {
            int goalEntranceCol = targetGoal.Col == 0 ? 1 : 10;
            var goalEntranceDefenders = _state.Pieces
                .Where(p => p.Team == opposingTeam && p.Position.Row == targetGoal.Row && p.Position.Col == goalEntranceCol)
                .ToList();

            if (goalEntranceDefenders.Any())
            {
                CreateInterceptOrShotDuel(ballHolder, goalEntranceDefenders, new Position(targetGoal.Row, goalEntranceCol), targetGoal, true);
                return GetCurrentState();
            }

            // ルート上に誰もいなければ直接ゴール！
            RegisterGoal(_state.ActiveTeam);
            return GetCurrentState();
        }

        // 通常パスの場合: 到着先マスに相手がいるか
        var defendersAtTarget = _state.Pieces.Where(p => p.Team == opposingTeam && p.Position.Row == targetPosition.Row && p.Position.Col == targetPosition.Col).ToList();
        if (defendersAtTarget.Any())
        {
            CreateInterceptOrShotDuel(ballHolder, defendersAtTarget, targetPosition, targetPosition, false);
            return GetCurrentState();
        }

        // オフサイド判定 (横進行 Col)
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
            _state.MatchLogs.Add($"[{_state.ActiveTeam}] {ballHolder.Name} から {receiver.Name} へのパスが通りました！");
        }
        else
        {
            _state.Ball.HolderPieceId = null;
            _state.MatchLogs.Add($"[{_state.ActiveTeam}] スペースへのスルーパス！ボールが ({targetPosition.Row}, {targetPosition.Col}) へ出されました。");
        }

        UpdateGkPrivilege();
        return GetCurrentState();
    }

    private void CreateInterceptOrShotDuel(Piece attacker, List<Piece> defenders, Position duelPos, Position targetPos, bool isShot)
    {
        var opposingTeam = _state.ActiveTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        _state.PendingDuel = new DuelContext
        {
            Type = isShot ? DuelType.Shot : DuelType.Intercept,
            DuelPosition = duelPos,
            PassTargetPosition = targetPos,
            AttackingTeam = _state.ActiveTeam,
            DefendingTeam = opposingTeam,
            Attackers = new List<DuelParticipant>
            {
                new() { PieceId = attacker.Id, Name = attacker.Name, Number = attacker.Number, Ability = attacker.Ability, IsGoalkeeper = attacker.IsGoalkeeper }
            },
            Defenders = defenders.Select(p => new DuelParticipant
            {
                PieceId = p.Id, Name = p.Name, Number = p.Number, Ability = p.Ability, IsGoalkeeper = p.IsGoalkeeper
            }).ToList(),
            Message = isShot
                ? $"【シュート！】 {attacker.Name} のシュートに対して相手ディフェンスがブロック勝負！"
                : $"【インターセプト発生！】 パスコースに相手選手が立ちふさがりました！"
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
        int kickoffCol = concededTeam == TeamType.TeamA ? 5 : 6;
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
