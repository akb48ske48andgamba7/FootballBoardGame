namespace FootballBoardGame.Server.Models;

public enum GamePhase
{
    SetupFirstHalfA,    // 前半: TeamA 自陣配置 (Row 1〜5)
    SetupFirstHalfB,    // 前半: TeamB 自陣配置 (Row 6〜10)
    FirstHalf,          // 前半試合進行中
    HalfTime,           // ハーフタイム (陣地交代)
    SetupSecondHalfA,   // 後半: TeamA 自陣配置 (Row 6〜10)
    SetupSecondHalfB,   // 後半: TeamB 自陣配置 (Row 1〜5)
    SecondHalf,         // 後半試合進行中
    GameOver            // 試合終了
}

public class GameState
{
    public Guid GameId { get; set; } = Guid.NewGuid();
    public GamePhase Phase { get; set; } = GamePhase.SetupFirstHalfA;
    public TeamType ActiveTeam { get; set; } = TeamType.TeamA;

    public int Half => (Phase == GamePhase.SetupFirstHalfA || Phase == GamePhase.SetupFirstHalfB || Phase == GamePhase.FirstHalf) ? 1 : 2;

    public int Turn { get; set; } = 1;                    // 1〜45
    public bool IsAdditionalTime { get; set; }
    public int AdditionalTimeTotal { get; set; }         // ダイス2個の合計 (2〜12)
    public int AdditionalTimeTurnsElapsed { get; set; }

    public int ScoreTeamA { get; set; }
    public int ScoreTeamB { get; set; }

    public List<Piece> Pieces { get; set; } = new();
    public Ball Ball { get; set; } = new();
    public TurnActionState CurrentTurnAction { get; set; } = new();

    public DuelContext? PendingDuel { get; set; }
    public List<string> MatchLogs { get; set; } = new();

    public string? OffsideWarning { get; set; }
}
