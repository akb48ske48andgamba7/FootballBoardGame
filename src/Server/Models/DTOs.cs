namespace FootballBoardGame.Server.Models;

public record PiecePlacementDto(string PieceId, int Row, int Col);

public record SetupTeamRequest(List<PiecePlacementDto> Placements);

public record MovePieceRequest(string PieceId, int TargetRow, int TargetCol);

public record PassOrShotRequest(int TargetRow, int TargetCol);

public record ApiResponse<T>(bool Success, string Message, T? Data);
