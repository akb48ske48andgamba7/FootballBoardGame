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

        // TeamBのDF最後尾を探す (GK除く)
        int? lastDfRow = _offsideService.GetOffsideLineRow(activeState, TeamType.TeamA);
        Assert.NotNull(lastDfRow);

        // 最後尾DFよりゴール側のマス(Row > lastDfRow)はオフサイド
        var offsidePos = new Position(lastDfRow.Value + 1, 3);
        bool isOffside = _offsideService.IsOffside(activeState, TeamType.TeamA, offsidePos);
        Assert.True(isOffside);

        // 同一ライン(Row == lastDfRow)はオフサイドではない
        var onsidePos = new Position(lastDfRow.Value, 3);
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
            gameEngine.MovePiece(fwPiece.Id, new Position(fwPiece.Position.Row + 3, fwPiece.Position.Col));
        });

        int originalRow = fwPiece.Position.Row;
        // 2マス移動は成功
        var updated = gameEngine.MovePiece(fwPiece.Id, new Position(originalRow + 2, fwPiece.Position.Col));
        Assert.Equal(originalRow + 2, updated.Pieces.First(p => p.Id == fwPiece.Id).Position.Row);
    }
}
