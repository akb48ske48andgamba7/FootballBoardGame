using FootballBoardGame.Server.Models;

namespace FootballBoardGame.Server.Services;

public interface IGameEngineService
{
    GameState GetCurrentState();
    GameState ResetGame();
    GameState SetupTeam(TeamType team, List<PiecePlacementDto> placements);
    GameState ApplyDefaultFormation(TeamType team);
    GameState MovePiece(string pieceId, Position targetPosition);
    GameState PassOrShot(Position targetPosition);
    GameState ResolveDuel();
    GameState EndTurn();
}
