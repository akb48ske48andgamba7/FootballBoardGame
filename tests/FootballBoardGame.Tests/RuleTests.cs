using FootballBoardGame.Server.Models;
using FootballBoardGame.Server.Services;
using Xunit;

namespace FootballBoardGame.Tests;

public class RuleTests
{
    private readonly IDiceService _diceService = new DiceService();
    private readonly IOffsideRuleService _offsideService = new OffsideRuleService();

    [Fact]
    public void TeamComposition_ShouldHaveCorrectAbilitiesAndGK()
    {
        var team = Piece.CreateDefaultTeam(TeamType.TeamA, 1);

        Assert.Equal(11, team.Count);
        Assert.Single(team.Where(p => p.Ability == 3)); // 能力3が1名
        Assert.Equal(3, team.Count(p => p.Ability == 2)); // 能力2が3名
        Assert.Equal(7, team.Count(p => p.Ability == 1)); // 能力1が7名
        Assert.Single(team.Where(p => p.IsGoalkeeper)); // GKが1名
    }

    [Fact]
    public void OffsideRule_ShouldDetectOffside_WhenReceiverIsPastLastDefender()
    {
        var gameEngine = new GameEngineService(_diceService, _offsideService);
        var state = gameEngine.GetCurrentState();

        // チームA、チームBの初期配置を確定して前半フェーズにする
        gameEngine.SetupTeam(TeamType.TeamA, state.Pieces.Where(p => p.Team == TeamType.TeamA)
            .Select(p => new PiecePlacementDto(p.Id, p.Position.Row, p.Position.Col)).ToList());
        gameEngine.SetupTeam(TeamType.TeamB, state.Pieces.Where(p => p.Team == TeamType.TeamB)
            .Select(p => new PiecePlacementDto(p.Id, p.Position.Row, p.Position.Col)).ToList());

        var activeState = gameEngine.GetCurrentState();
        Assert.Equal(GamePhase.FirstHalf, activeState.Phase);

        // TeamBのDF最後尾の列(Col)を探す (GK除く)
        int? lastDfCol = _offsideService.GetOffsideLineCol(activeState, TeamType.TeamA);
        Assert.NotNull(lastDfCol);

        // 最後尾DFより相手ゴール側 (Col > lastDfCol) はオフサイド
        var offsidePos = new Position(4, lastDfCol.Value + 1);
        bool isOffside = _offsideService.IsOffside(activeState, TeamType.TeamA, offsidePos);
        Assert.True(isOffside);

        // 同一列 (Col == lastDfCol) はオフサイドではない
        var onsidePos = new Position(4, lastDfCol.Value);
        bool isOnside = _offsideService.IsOffside(activeState, TeamType.TeamA, onsidePos);
        Assert.False(isOnside);
    }

    [Fact]
    public void Move_ShouldEnforceMaxTwoSquares()
    {
        var gameEngine = new GameEngineService(_diceService, _offsideService);
        var state = gameEngine.GetCurrentState();
        gameEngine.SetupTeam(TeamType.TeamA, state.Pieces.Where(p => p.Team == TeamType.TeamA)
            .Select(p => new PiecePlacementDto(p.Id, p.Position.Row, p.Position.Col)).ToList());
        gameEngine.SetupTeam(TeamType.TeamB, state.Pieces.Where(p => p.Team == TeamType.TeamB)
            .Select(p => new PiecePlacementDto(p.Id, p.Position.Row, p.Position.Col)).ToList());

        var fwPiece = gameEngine.GetCurrentState().Pieces.First(p => p.Team == TeamType.TeamA && p.Number == 10);

        // 3マス移動はエラー
        Assert.Throws<InvalidOperationException>(() =>
        {
            gameEngine.MovePiece(fwPiece.Id, new Position(fwPiece.Position.Row, fwPiece.Position.Col + 3));
        });

        int originalCol = fwPiece.Position.Col;
        // 2マス移動は成功
        var updated = gameEngine.MovePiece(fwPiece.Id, new Position(fwPiece.Position.Row, originalCol + 2));
        Assert.Equal(originalCol + 2, updated.Pieces.First(p => p.Id == fwPiece.Id).Position.Col);
    }

    [Fact]
    public void CpuAi_ShouldAutoSetupAndTakeTurn()
    {
        var gameEngine = new GameEngineService(_diceService, _offsideService);
        var cpuService = new CpuAiService(gameEngine, _offsideService);

        var state = gameEngine.GetCurrentState();
        // TeamA プレイヤー配置
        gameEngine.SetupTeam(TeamType.TeamA, state.Pieces.Where(p => p.Team == TeamType.TeamA)
            .Select(p => new PiecePlacementDto(p.Id, p.Position.Row, p.Position.Col)).ToList());

        // CPU (TeamB) の自動配置実行
        var setupState = cpuService.ExecuteCpuStep(TeamType.TeamB);
        Assert.Equal(GamePhase.FirstHalf, setupState.Phase);
        Assert.Equal(TeamType.TeamA, setupState.ActiveTeam);

        // TeamA の手番を終了して TeamB (CPU) の手番へ
        gameEngine.EndTurn();
        var cpuTurnState = gameEngine.GetCurrentState();
        Assert.Equal(TeamType.TeamB, cpuTurnState.ActiveTeam);

        // CPUの行動ステップを実行
        var afterCpuState = cpuService.ExecuteCpuStep(TeamType.TeamB);
        // CPUが行動してターンを終了し、手番がTeamAに戻るか、あるいはデュエルが発生していることを確認
        Assert.True(afterCpuState.ActiveTeam == TeamType.TeamA || afterCpuState.PendingDuel != null);
    }

    [Fact]
    public void TurnActions_ShouldAllowUpToThreeMovesAndTwoPassOrShots()
    {
        var gameEngine = new GameEngineService(_diceService, _offsideService);
        var state = gameEngine.GetCurrentState();
        gameEngine.SetupTeam(TeamType.TeamA, state.Pieces.Where(p => p.Team == TeamType.TeamA)
            .Select(p => new PiecePlacementDto(p.Id, p.Position.Row, p.Position.Col)).ToList());
        gameEngine.SetupTeam(TeamType.TeamB, state.Pieces.Where(p => p.Team == TeamType.TeamB)
            .Select(p => new PiecePlacementDto(p.Id, p.Position.Row, p.Position.Col)).ToList());

        var teamAPieces = gameEngine.GetCurrentState().Pieces.Where(p => p.Team == TeamType.TeamA).ToList();

        // 3人の異なる駒を移動
        gameEngine.MovePiece(teamAPieces[0].Id, new Position(teamAPieces[0].Position.Row + 1, teamAPieces[0].Position.Col));
        gameEngine.MovePiece(teamAPieces[1].Id, new Position(teamAPieces[1].Position.Row + 1, teamAPieces[1].Position.Col));
        gameEngine.MovePiece(teamAPieces[2].Id, new Position(teamAPieces[2].Position.Row + 1, teamAPieces[2].Position.Col));

        // 4人目の移動は例外
        Assert.Throws<InvalidOperationException>(() =>
        {
            gameEngine.MovePiece(teamAPieces[3].Id, new Position(teamAPieces[3].Position.Row + 1, teamAPieces[3].Position.Col));
        });

        var turnState = gameEngine.GetCurrentState().CurrentTurnAction;
        Assert.Equal(3, turnState.MovedPieceIds.Count);
        Assert.Equal(0, turnState.StandardMovesRemaining);
        Assert.Equal(2, turnState.RemainingPassOrShots);
    }

    [Fact]
    public void Goalkeeper_InPenaltyArea_ShouldTriggerShotDuelAndGetAbilityPlusOne()
    {
        var gameEngine = new GameEngineService(_diceService, _offsideService);
        var state = gameEngine.GetCurrentState();
        gameEngine.SetupTeam(TeamType.TeamA, state.Pieces.Where(p => p.Team == TeamType.TeamA)
            .Select(p => new PiecePlacementDto(p.Id, p.Position.Row, p.Position.Col)).ToList());
        gameEngine.SetupTeam(TeamType.TeamB, state.Pieces.Where(p => p.Team == TeamType.TeamB)
            .Select(p => new PiecePlacementDto(p.Id, p.Position.Row, p.Position.Col)).ToList());

        var currentState = gameEngine.GetCurrentState();

        // TeamBのGKを (Row 3, Col 12) に配置 (ペナルティーエリア内だがゴールの真ん前 Row 4 ではない)
        var bGk = currentState.Pieces.First(p => p.Team == TeamType.TeamB && p.IsGoalkeeper);
        bGk.Position = new Position(3, 12);

        // ボール保持者を TeamA の選手にして (Row 4, Col 10) に配置 (直線上にGKはいない)
        var shooter = currentState.Pieces.First(p => p.Team == TeamType.TeamA && p.Number == 10);
        shooter.Position = new Position(4, 10);
        currentState.Ball.HolderPieceId = shooter.Id;
        currentState.Ball.Position = shooter.Position;

        // シュート実行 (GoalB: Row 4, Col 13)
        var afterShotState = gameEngine.PassOrShot(Position.GoalB);

        // 直線上にGKはいなくても、ペナルティーエリア内にいるため勝負が発生する
        Assert.NotNull(afterShotState.PendingDuel);
        var duel = afterShotState.PendingDuel;
        Assert.Equal(DuelType.Shot, duel.Type);
        Assert.Contains(duel.Defenders, d => d.PieceId == bGk.Id);
        Assert.True(duel.HasGkHandBonus);

        var gkParticipant = duel.Defenders.First(d => d.PieceId == bGk.Id);
        Assert.Equal(1, gkParticipant.AbilityBonus);
        Assert.Equal(bGk.Ability + 1, gkParticipant.TotalAbility);
    }

    [Fact]
    public void FormationPresets_ShouldContain15Formations_AndValidCoordinates()
    {
        Assert.Equal(15, FormationPreset.All.Count);

        foreach (var preset in FormationPreset.All)
        {
            Assert.Equal(11, preset.Positions.Count);
            // GKは必ず (Row 4, Col 1)
            var gk = preset.Positions.First(p => p.Number == 1);
            Assert.Equal(4, gk.Row);
            Assert.Equal(1, gk.RelativeCol);

            // 全ての駒が自陣内 (Row 1〜7, RelCol 1〜6)
            foreach (var pos in preset.Positions)
            {
                Assert.InRange(pos.Row, 1, 7);
                Assert.InRange(pos.RelativeCol, 1, 6);
            }
        }
    }

    [Fact]
    public void SetupTeam_WithCustomAbilities_ShouldApplyAndValidateCorrectly()
    {
        var gameEngine = new GameEngineService(_diceService, _offsideService);
        var state = gameEngine.GetCurrentState();
        var teamAPieces = state.Pieces.Where(p => p.Team == TeamType.TeamA).OrderBy(p => p.Number).ToList();

        // 正常な能力割り当て: GKに★3(1名), DFに★2(3名), その他★1(7名)
        var validPlacements = new List<PiecePlacementDto>
        {
            new(teamAPieces[0].Id, 4, 1, 3), // GKを★3にカスタム
            new(teamAPieces[1].Id, 2, 2, 2), // DFに★2
            new(teamAPieces[2].Id, 3, 2, 2), // DFに★2
            new(teamAPieces[3].Id, 5, 2, 2), // DFに★2
            new(teamAPieces[4].Id, 6, 2, 1),
            new(teamAPieces[5].Id, 2, 4, 1),
            new(teamAPieces[6].Id, 4, 4, 1),
            new(teamAPieces[7].Id, 6, 4, 1),
            new(teamAPieces[8].Id, 3, 6, 1),
            new(teamAPieces[9].Id, 4, 6, 1), // 元のエースを★1に変更
            new(teamAPieces[10].Id, 5, 6, 1),
        };

        var updated = gameEngine.SetupTeam(TeamType.TeamA, validPlacements);
        var updatedPieces = updated.Pieces.Where(p => p.Team == TeamType.TeamA).ToList();

        Assert.Equal(3, updatedPieces.First(p => p.Number == 1).Ability); // GKが★3になったことを確認
        Assert.Equal(1, updatedPieces.Count(p => p.Ability == 3));
        Assert.Equal(3, updatedPieces.Count(p => p.Ability == 2));
        Assert.Equal(7, updatedPieces.Count(p => p.Ability == 1));

        // 不正な能力割り当て（★3が2名いる場合）は例外が発生することを確認
        var invalidPlacements = new List<PiecePlacementDto>
        {
            new(teamAPieces[0].Id, 4, 1, 3),
            new(teamAPieces[1].Id, 2, 2, 3), // もう1名★3 (不正)
            new(teamAPieces[2].Id, 3, 2, 2),
            new(teamAPieces[3].Id, 5, 2, 2),
            new(teamAPieces[4].Id, 6, 2, 1),
            new(teamAPieces[5].Id, 2, 4, 1),
            new(teamAPieces[6].Id, 4, 4, 1),
            new(teamAPieces[7].Id, 6, 4, 1),
            new(teamAPieces[8].Id, 3, 6, 1),
            new(teamAPieces[9].Id, 4, 6, 1),
            new(teamAPieces[10].Id, 5, 6, 1),
        };

        Assert.Throws<ArgumentException>(() =>
        {
            gameEngine.SetupTeam(TeamType.TeamB, invalidPlacements);
        });

        // 能力2が4名設定できることの確認 (★3が1名, ★2が4名, ★1が6名)
        var fourStar2Placements = new List<PiecePlacementDto>
        {
            new(teamAPieces[0].Id, 4, 1, 2), // GK ★2
            new(teamAPieces[1].Id, 2, 2, 2), // DF ★2
            new(teamAPieces[2].Id, 3, 2, 2), // DF ★2
            new(teamAPieces[3].Id, 5, 2, 2), // DF ★2 (計4名)
            new(teamAPieces[4].Id, 6, 2, 1),
            new(teamAPieces[5].Id, 2, 4, 1),
            new(teamAPieces[6].Id, 4, 4, 1),
            new(teamAPieces[7].Id, 6, 4, 1),
            new(teamAPieces[8].Id, 3, 6, 1),
            new(teamAPieces[9].Id, 4, 6, 3), // FW ★3 エース
            new(teamAPieces[10].Id, 5, 6, 1),
        };

        var updatedFour = gameEngine.SetupTeam(TeamType.TeamA, fourStar2Placements);
        var piecesFour = updatedFour.Pieces.Where(p => p.Team == TeamType.TeamA).ToList();
        Assert.Equal(1, piecesFour.Count(p => p.Ability == 3));
        Assert.Equal(4, piecesFour.Count(p => p.Ability == 2));
        Assert.Equal(6, piecesFour.Count(p => p.Ability == 1));

        // 能力2が5名の場合はエラーになることを確認
        var fiveStar2Placements = new List<PiecePlacementDto>
        {
            new(teamAPieces[0].Id, 4, 1, 2), // GK ★2
            new(teamAPieces[1].Id, 2, 2, 2), // DF ★2
            new(teamAPieces[2].Id, 3, 2, 2), // DF ★2
            new(teamAPieces[3].Id, 5, 2, 2), // DF ★2
            new(teamAPieces[4].Id, 6, 2, 2), // DF ★2 (計5名: 不正)
            new(teamAPieces[5].Id, 2, 4, 1),
            new(teamAPieces[6].Id, 4, 4, 1),
            new(teamAPieces[7].Id, 6, 4, 1),
            new(teamAPieces[8].Id, 3, 6, 1),
            new(teamAPieces[9].Id, 4, 6, 3), // FW ★3 エース
            new(teamAPieces[10].Id, 5, 6, 1),
        };

        Assert.Throws<ArgumentException>(() =>
        {
            gameEngine.SetupTeam(TeamType.TeamA, fiveStar2Placements);
        });
    }

    [Fact]
    public void MultipleMoves_SamePiece_ShouldBeAllowedUpToMaxMoves()
    {
        var gameEngine = new GameEngineService(_diceService, _offsideService);
        var state = gameEngine.GetCurrentState();
        gameEngine.SetupTeam(TeamType.TeamA, state.Pieces.Where(p => p.Team == TeamType.TeamA)
            .Select(p => new PiecePlacementDto(p.Id, p.Position.Row, p.Position.Col)).ToList());
        gameEngine.SetupTeam(TeamType.TeamB, state.Pieces.Where(p => p.Team == TeamType.TeamB)
            .Select(p => new PiecePlacementDto(p.Id, p.Position.Row, p.Position.Col)).ToList());

        var fwPiece = gameEngine.GetCurrentState().Pieces.First(p => p.Team == TeamType.TeamA && p.Number == 10);

        // 1回目の移動
        gameEngine.MovePiece(fwPiece.Id, new Position(fwPiece.Position.Row, fwPiece.Position.Col + 1));
        Assert.Equal(2, gameEngine.GetCurrentState().CurrentTurnAction.StandardMovesRemaining);

        // 2回目の移動 (同一選手！)
        gameEngine.MovePiece(fwPiece.Id, new Position(fwPiece.Position.Row, fwPiece.Position.Col + 1));
        Assert.Equal(1, gameEngine.GetCurrentState().CurrentTurnAction.StandardMovesRemaining);

        // 3回目の移動 (同一選手！)
        gameEngine.MovePiece(fwPiece.Id, new Position(fwPiece.Position.Row, fwPiece.Position.Col + 1));
        Assert.Equal(0, gameEngine.GetCurrentState().CurrentTurnAction.StandardMovesRemaining);

        // 4回目の移動はエラー
        Assert.Throws<InvalidOperationException>(() =>
        {
            gameEngine.MovePiece(fwPiece.Id, new Position(fwPiece.Position.Row, fwPiece.Position.Col + 1));
        });
    }

    [Fact]
    public void ExpandedPenaltyArea_ShouldCoverTwoColumns()
    {
        // TeamAが左(Col 0が自ゴール)、TeamBが右(Col 13が自ゴール)
        // TeamA守備時: Col 1〜2, Row 3〜5 がペナルティエリア
        Assert.True(new Position(3, 1).IsInPenaltyArea(TeamType.TeamA, 1));
        Assert.True(new Position(4, 1).IsInPenaltyArea(TeamType.TeamA, 1));
        Assert.True(new Position(5, 1).IsInPenaltyArea(TeamType.TeamA, 1));
        Assert.True(new Position(3, 2).IsInPenaltyArea(TeamType.TeamA, 1)); // 拡大された列
        Assert.True(new Position(4, 2).IsInPenaltyArea(TeamType.TeamA, 1)); // 拡大された列
        Assert.True(new Position(5, 2).IsInPenaltyArea(TeamType.TeamA, 1)); // 拡大された列

        // エリア外
        Assert.False(new Position(2, 1).IsInPenaltyArea(TeamType.TeamA, 1)); // 上ハーフ
        Assert.False(new Position(6, 2).IsInPenaltyArea(TeamType.TeamA, 1)); // 下ハーフ
        Assert.False(new Position(4, 3).IsInPenaltyArea(TeamType.TeamA, 1)); // Col 3

        // TeamB守備時: Col 11〜12, Row 3〜5
        Assert.True(new Position(4, 12).IsInPenaltyArea(TeamType.TeamB, 1));
        Assert.True(new Position(4, 11).IsInPenaltyArea(TeamType.TeamB, 1)); // 拡大された列
        Assert.False(new Position(4, 10).IsInPenaltyArea(TeamType.TeamB, 1));
    }
}


