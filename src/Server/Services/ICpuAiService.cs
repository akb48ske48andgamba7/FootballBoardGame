using FootballBoardGame.Server.Models;

namespace FootballBoardGame.Server.Services;

public interface ICpuAiService
{
    GameState ExecuteCpuStep(TeamType cpuTeam = TeamType.TeamB);
}
