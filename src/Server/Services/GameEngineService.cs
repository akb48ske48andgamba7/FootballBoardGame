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
        // リアルタイムのオフサイドライン行番号を計算して付与
        int? offsideRow = _offsideService.GetOffsideLineRow(_state, _state.ActiveTeam);
        if (offsideRow.HasValue)
        {
            _state.OffsideWarning = $"Offside Line: Row {offsideRow.Value}";
        }
        else
        {
            _state.OffsideWarning = null;
        }

        return _state;
    }

    public GameState ResetGame()
    {
        _state = new GameState
        {
            GameId = Guid.NewGuid(),
            Phase = GamePhase.SetupFirstHalfA,
            ActiveTeam = TeamType.TeamA,
            Turn = 1,
            IsAdditionalTime = false,
            AdditionalTimeTotal = 0,
            AdditionalTimeTurnsElapsed = 0,
            ScoreTeamA = 0,
            ScoreTeamB = 0,
            Pieces = new List<Piece>(),
            Ball = new Ball { Position = new Position(5, 3), HolderPieceId = null },
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

        // 自陣のベース行（前半: TeamAは1〜5、TeamBは6〜10 / 後半は逆）
        bool isTopHalf = (half == 1 && isTeamA) || (half == 2 && !isTeamA);

        int gkRow = isTopHalf ? 1 : 10;
        int dfRow = isTopHalf ? 2 : 9;
        int mf1Row = isTopHalf ? 3 : 8;
        int mf2Row = isTopHalf ? 4 : 7;
        int fwRow = isTopHalf ? 5 : 6;

        var teamPieces = _state.Pieces.Where(p => p.Team == team).OrderBy(p => p.Number).ToList();

        foreach (var p in teamPieces)
        {
            switch (p.Number)
            {
                case 1: // GK
                    p.Position = new Position(gkRow, 3);
                    break;
                case 2: // DF Left
                    p.Position = new Position(dfRow, 1);
                    break;
                case 3: // DF Center-Left
                    p.Position = new Position(dfRow, 2);
                    break;
                case 4: // DF Center-Right
                    p.Position = new Position(dfRow, 4);
                    break;
                case 5: // DF Right
                    p.Position = new Position(dfRow, 5);
                    break;
                case 6: // MF Left
                    p.Position = new Position(mf1Row, 2);
                    break;
                case 7: // MF Right
                    p.Position = new Position(mf1Row, 4);
                    break;
                case 8: // MF Center
                    p.Position = new Position(mf2Row, 3);
                    break;
                case 9: // FW Left
                    p.Position = new Position(fwRow, 2);
                    break;
                case 10: // FW Center (Ace ★3)
                    p.Position = new Position(fwRow, 3);
                    break;
                case 11: // FW Right
                    p.Position = new Position(fwRow, 4);
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
        bool isTopHalf = (half == 1 && team == TeamType.TeamA) || (half == 2 && team == TeamType.TeamB);
        int minRow = isTopHalf ? 1 : 6;
        int maxRow = isTopHalf ? 5 : 10;

        foreach (var placement in placements)
        {
            if (placement.Row < minRow || placement.Row > maxRow || placement.Col < 1 || placement.Col > 5)
            {
                throw new ArgumentException($"自陣 (Row {minRow}〜{maxRow}, Col 1〜5) の範囲内に配置してください。");
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
        // 始点から近い順にチェック
        foreach (var step in intermediatePath)
        {
            var defendersAtStep = _state.Pieces.Where(p => p.Team == opposingTeam && p.Position.Row == step.Row && p.Position.Col == step.Col).ToList();
            if (defendersAtStep.Any())
            {
                // パスカット / シュート阻止デュエル発生
                CreateInterceptOrShotDuel(ballHolder, defendersAtStep, step, targetPosition, isShot);
                return GetCurrentState();
            }
        }

        // シュートの場合、ゴール枠直前や目標位置に相手GK/DFがいればブロック勝負
        if (isShot)
        {
            // ゴールマス手前（1マス前）または経路上に相手GK/DFがいるか
            // ゴール直前マスの敵
            int goalEntranceRow = targetGoal.Row == 0 ? 1 : 10;
            var goalEntranceDefenders = _state.Pieces
                .Where(p => p.Team == opposingTeam && p.Position.Row == goalEntranceRow && p.Position.Col == targetGoal.Col)
                .ToList();

            if (goalEntranceDefenders.Any())
            {
                CreateInterceptOrShotDuel(ballHolder, goalEntranceDefenders, new Position(goalEntranceRow, targetGoal.Col), targetGoal, true);
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

        // オフサイド判定
        // パスが出た瞬間に、受け手となる選手が「相手最後尾DF（GK除く）の横ライン」よりも相手ゴール側のマスにいた場合
        var receiver = _state.Pieces.FirstOrDefault(p => p.Team == _state.ActiveTeam && p.Position.Row == targetPosition.Row && p.Position.Col == targetPosition.Col);
        if (receiver != null && _offsideService.IsOffside(_state, _state.ActiveTeam, targetPosition))
        {
            // オフサイド！
            _state.MatchLogs.Add($"【オフサイド！】 [{_state.ActiveTeam}] のパス受け手がオフサイドラインを越えていました。");
            _state.Ball.HolderPieceId = null;
            _state.Ball.Position = targetPosition;
            // 相手ボールでターン終了
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

        // 判定: 能力値合計 + サイコロの目。数値が大きいチームが勝利（同点の場合は守備側有利）
        bool attackerWins = duel.AttackerTotal > duel.DefenderTotal;
        duel.Winner = attackerWins ? duel.AttackingTeam : duel.DefendingTeam;

        _state.MatchLogs.Add($"[サイコロ判定] 攻撃側: 能力{duel.AttackerAbilitySum} + 出目{duel.AttackerDice} = {duel.AttackerTotal} VS 守備側: 能力{duel.DefenderAbilitySum} + 出目{duel.DefenderDice} = {duel.DefenderTotal}");

        if (duel.Type == DuelType.Tackle)
        {
            if (attackerWins)
            {
                // タックル成功！仕掛けたアタッカーがボールを奪取
                var mainAttacker = _state.Pieces.First(p => p.Id == duel.Attackers[0].PieceId);
                _state.Ball.HolderPieceId = mainAttacker.Id;
                _state.Ball.Position = duel.DuelPosition;
                _state.MatchLogs.Add($"【タックル成功！】 [{duel.AttackingTeam}] {mainAttacker.Name} がボールを奪いました！");
            }
            else
            {
                // タックル失敗！ボール保持者がキープ
                _state.MatchLogs.Add($"【タックル失敗！】 [{duel.DefendingTeam}] が体を張ってボールをキープ！");
            }
        }
        else if (duel.Type == DuelType.Intercept)
        {
            if (attackerWins)
            {
                // パス通過！
                _state.MatchLogs.Add($"【パス通過！】 パスがディフェンスの頭上を越えて通りました！");
                if (duel.PassTargetPosition != null)
                {
                    _state.Ball.Position = duel.PassTargetPosition;
                    var receiver = _state.Pieces.FirstOrDefault(p => p.Team == duel.AttackingTeam && p.Position.Row == duel.PassTargetPosition.Row && p.Position.Col == duel.PassTargetPosition.Col);
                    _state.Ball.HolderPieceId = receiver?.Id;
                }
            }
            else
            {
                // インターセプト成功！ディフェンダーがボールを奪取
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
                // ゴール！！
                RegisterGoal(duel.AttackingTeam);
            }
            else
            {
                // セーブ / ブロック成功
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

        // キックオフリスタート（失点したチームのセンターサークルから）
        var concededTeam = scoringTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        _state.Ball.Position = new Position(5, 3);
        var kickOffPlayer = _state.Pieces.FirstOrDefault(p => p.Team == concededTeam && p.Number == 10);
        if (kickOffPlayer != null)
        {
            kickOffPlayer.Position = new Position(concededTeam == TeamType.TeamA ? 5 : 6, 3);
            _state.Ball.HolderPieceId = kickOffPlayer.Id;
        }
        else
        {
            _state.Ball.HolderPieceId = null;
        }

        // ターン終了とし、失点したチームの手番へ
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

        // 攻守交代 (TeamA <-> TeamB)
        if (_state.ActiveTeam == TeamType.TeamA)
        {
            _state.ActiveTeam = TeamType.TeamB;
            StartNewTurn();
            _state.MatchLogs.Add($"手番交代: [{_state.ActiveTeam}] のターンです。");
            return GetCurrentState();
        }

        // TeamBのターン終了時 = 1ラウンド（1ターン）経過
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
                // 45ターン終了！アディショナルタイム判定（サイコロ2個）
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
            // アディショナルタイム中
            _state.AdditionalTimeTurnsElapsed++;
            if (_state.AdditionalTimeTurnsElapsed < _state.AdditionalTimeTotal)
            {
                _state.MatchLogs.Add($"アディショナルタイム進行中 ({_state.AdditionalTimeTurnsElapsed}/{_state.AdditionalTimeTotal})");
                StartNewTurn();
            }
            else
            {
                // 前半または後半終了
                if (_state.Phase == GamePhase.FirstHalf)
                {
                    _state.Phase = GamePhase.HalfTime;
                    _state.Turn = 1;
                    _state.IsAdditionalTime = false;
                    _state.AdditionalTimeTotal = 0;
                    _state.AdditionalTimeTurnsElapsed = 0;
                    _state.MatchLogs.Add($"【前半終了！】 ハーフタイムです。陣地を交代して後半の配置を行ってください。");
                    // 陣地交代に合わせてデフォルト配置を適用
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
        // GKがボールを保持しているターンに限り、GK自身を追加で移動可能
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
