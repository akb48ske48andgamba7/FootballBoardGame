using FootballBoardGame.Server.Models;
using FootballBoardGame.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace FootballBoardGame.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly IGameEngineService _gameEngine;
    private readonly ICpuAiService _cpuAiService;

    public GameController(IGameEngineService gameEngine, ICpuAiService cpuAiService)
    {
        _gameEngine = gameEngine;
        _cpuAiService = cpuAiService;
    }

    [HttpGet]
    public ActionResult<ApiResponse<GameState>> GetState()
    {
        return Ok(new ApiResponse<GameState>(true, "ゲーム状態を取得しました。", _gameEngine.GetCurrentState()));
    }

    [HttpPost("mode")]
    public ActionResult<ApiResponse<GameState>> SetGameMode([FromBody] SetGameModeRequest request)
    {
        var state = _gameEngine.SetGameMode(request.Mode);
        return Ok(new ApiResponse<GameState>(true, "対戦モードを更新しました。", state));
    }

    [HttpPost("cpu/step")]
    public ActionResult<ApiResponse<GameState>> ExecuteCpuStep()
    {
        try
        {
            var state = _cpuAiService.ExecuteCpuStep();
            return Ok(new ApiResponse<GameState>(true, "CPUの手番を実行しました。", state));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<GameState>(false, ex.Message, null));
        }
    }

    [HttpPost("reset")]
    public ActionResult<ApiResponse<GameState>> ResetGame()
    {
        var state = _gameEngine.ResetGame();
        return Ok(new ApiResponse<GameState>(true, "ゲームをリセットしました。", state));
    }

    [HttpPost("setup/default")]
    public ActionResult<ApiResponse<GameState>> ApplyDefaultFormation([FromQuery] TeamType team)
    {
        try
        {
            var state = _gameEngine.ApplyDefaultFormation(team);
            return Ok(new ApiResponse<GameState>(true, $"{team} のデフォルト配置を適用しました。", state));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<GameState>(false, ex.Message, null));
        }
    }

    [HttpPost("setup")]
    public ActionResult<ApiResponse<GameState>> SetupTeam([FromQuery] TeamType team, [FromBody] SetupTeamRequest request)
    {
        try
        {
            var state = _gameEngine.SetupTeam(team, request.Placements);
            return Ok(new ApiResponse<GameState>(true, $"{team} の配置を確定しました。", state));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<GameState>(false, ex.Message, null));
        }
    }

    [HttpPost("move")]
    public ActionResult<ApiResponse<GameState>> MovePiece([FromBody] MovePieceRequest request)
    {
        try
        {
            var state = _gameEngine.MovePiece(request.PieceId, new Position(request.TargetRow, request.TargetCol));
            return Ok(new ApiResponse<GameState>(true, "コマを移動しました。", state));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<GameState>(false, ex.Message, null));
        }
    }

    [HttpPost("pass")]
    public ActionResult<ApiResponse<GameState>> PassOrShot([FromBody] PassOrShotRequest request)
    {
        try
        {
            var state = _gameEngine.PassOrShot(new Position(request.TargetRow, request.TargetCol));
            return Ok(new ApiResponse<GameState>(true, "パスまたはシュートを実行しました。", state));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<GameState>(false, ex.Message, null));
        }
    }

    [HttpPost("duel/resolve")]
    public ActionResult<ApiResponse<GameState>> ResolveDuel()
    {
        try
        {
            var state = _gameEngine.ResolveDuel();
            return Ok(new ApiResponse<GameState>(true, "サイコロ勝負を判定しました。", state));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<GameState>(false, ex.Message, null));
        }
    }

    [HttpPost("end-turn")]
    public ActionResult<ApiResponse<GameState>> EndTurn()
    {
        try
        {
            var state = _gameEngine.EndTurn();
            return Ok(new ApiResponse<GameState>(true, "ターンを終了しました。", state));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<GameState>(false, ex.Message, null));
        }
    }
}
