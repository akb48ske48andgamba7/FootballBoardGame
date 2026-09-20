namespace FootballBoardGame.Server.Models;

public class Ball
{
    public Position Position { get; set; } = new(5, 3);
    public string? HolderPieceId { get; set; }

    public bool IsHeld => !string.IsNullOrEmpty(HolderPieceId);
}
