namespace FootballBoardGame.Server.Models;

public enum TeamType
{
    TeamA,
    TeamB
}

public class Piece
{
    public string Id { get; set; } = string.Empty;
    public TeamType Team { get; set; }
    public int Number { get; set; }              // 1〜11
    public string Name { get; set; } = string.Empty;
    public int Ability { get; set; }             // 1, 2, 3
    public bool IsGoalkeeper { get; set; }
    public Position Position { get; set; } = new(1, 1);

    public static List<Piece> CreateDefaultTeam(TeamType team, int half = 1)
    {
        // チーム構成: 能力3が1名, 能力2が3名, 能力1が7名。1名がGK。
        // GKは背番号1 (能力2に設定、または能力1/2/3の好みに応じ、ここでは能力2をGK、能力3を10番、他バランス良く配置)
        // 合計: 能力3×1 (No.10), 能力2×3 (No.1 GK, No.7 MF, No.9 FW), 能力1×7 (No.2,3,4,5 DF, No.6,8 MF, No.11 FW)
        var pieces = new List<Piece>();

        // GK
        pieces.Add(new Piece
        {
            Id = $"{(team == TeamType.TeamA ? "A" : "B")}-1",
            Team = team,
            Number = 1,
            Name = "GK",
            Ability = 2,
            IsGoalkeeper = true,
            Position = new Position(1, 3) // 初期値は後でフォーメーションで上書き
        });

        // DF (能力1 × 4)
        for (int i = 2; i <= 5; i++)
        {
            pieces.Add(new Piece
            {
                Id = $"{(team == TeamType.TeamA ? "A" : "B")}-{i}",
                Team = team,
                Number = i,
                Name = $"DF {i}",
                Ability = 1,
                IsGoalkeeper = false,
                Position = new Position(2, i - 1)
            });
        }

        // MF (能力1 × 2, 能力2 × 1)
        pieces.Add(new Piece
        {
            Id = $"{(team == TeamType.TeamA ? "A" : "B")}-6",
            Team = team,
            Number = 6,
            Name = "MF 6",
            Ability = 1,
            IsGoalkeeper = false,
            Position = new Position(3, 2)
        });
        pieces.Add(new Piece
        {
            Id = $"{(team == TeamType.TeamA ? "A" : "B")}-7",
            Team = team,
            Number = 7,
            Name = "MF 7",
            Ability = 2,
            IsGoalkeeper = false,
            Position = new Position(3, 4)
        });
        pieces.Add(new Piece
        {
            Id = $"{(team == TeamType.TeamA ? "A" : "B")}-8",
            Team = team,
            Number = 8,
            Name = "MF 8",
            Ability = 1,
            IsGoalkeeper = false,
            Position = new Position(4, 3)
        });

        // FW (能力2 × 1, 能力3 × 1(エース), 能力1 × 1)
        pieces.Add(new Piece
        {
            Id = $"{(team == TeamType.TeamA ? "A" : "B")}-9",
            Team = team,
            Number = 9,
            Name = "FW 9",
            Ability = 2,
            IsGoalkeeper = false,
            Position = new Position(5, 2)
        });
        pieces.Add(new Piece
        {
            Id = $"{(team == TeamType.TeamA ? "A" : "B")}-10",
            Team = team,
            Number = 10,
            Name = "FW 10 ★",
            Ability = 3, // エース
            IsGoalkeeper = false,
            Position = new Position(5, 3)
        });
        pieces.Add(new Piece
        {
            Id = $"{(team == TeamType.TeamA ? "A" : "B")}-11",
            Team = team,
            Number = 11,
            Name = "FW 11",
            Ability = 1,
            IsGoalkeeper = false,
            Position = new Position(5, 4)
        });

        return pieces;
    }
}
