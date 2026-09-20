namespace FootballBoardGame.Server.Models;

public enum TeamType
{
    TeamA,
    TeamB
}

/// <summary>
/// 【学習用解説: サッカー選手（コマ）を表すエンティティクラス】
/// 
/// 将棋やチェスにおける「駒」に相当し、サッカーの各選手が持つ固有データ（背番号、能力、ポジション）を保持します。
/// 
/// ■ 設計のポイント:
/// - `Ability` (能力値 1〜3): サイコロ勝負（出目 1〜6）に加算される固定値です。★3のエースはサイコロで大きなアドバンテージを持ちます。
/// - `IsGoalkeeper`: ゴールキーパーフラグ。ペナルティエリア内でシュートを打たれた際、手を使った守備として能力+1ボーナスを得られます。
/// - `KickoffPosition`: ゴールが決まった後、実際のサッカー同様に初期陣形へ戻るための定位置を記憶します。
/// </summary>
public class Piece
{
    public string Id { get; set; } = string.Empty;
    public TeamType Team { get; set; }
    public int Number { get; set; }              // 背番号 (1〜11)
    public string Name { get; set; } = string.Empty;
    public int Ability { get; set; }             // 能力値: 1 (★1), 2 (★2), 3 (★3: エース)
    public bool IsGoalkeeper { get; set; }       // GKフラグ
    public Position Position { get; set; } = new(4, 1); // 現在のピッチ上の座標
    
    /// <summary>
    /// キックオフ時の初期フォーメーション配置（ゴール後に全員が元のポジションへ戻るために使用）
    /// </summary>
    public Position? KickoffPosition { get; set; }

    /// <summary>
    /// デフォルトのスターティングイレブン（11名）を生成するファクトリメソッドです。
    /// </summary>
    public static List<Piece> CreateDefaultTeam(TeamType team, int half = 1)
    {
        // チーム構成: 能力3が1名, 能力2が3名, 能力1が7名。1名がGK。
        var pieces = new List<Piece>();
        string prefix = team == TeamType.TeamA ? "A" : "B";

        // GK (能力2)
        pieces.Add(new Piece
        {
            Id = $"{prefix}-1",
            Team = team,
            Number = 1,
            Name = "GK",
            Ability = 2,
            IsGoalkeeper = true,
            Position = new Position(4, 1)
        });

        // DF (能力1 × 4)
        for (int i = 2; i <= 5; i++)
        {
            pieces.Add(new Piece
            {
                Id = $"{prefix}-{i}",
                Team = team,
                Number = i,
                Name = $"DF {i}",
                Ability = 1,
                IsGoalkeeper = false,
                Position = new Position(i, 2)
            });
        }

        // MF (能力1 × 2, 能力2 × 1)
        pieces.Add(new Piece
        {
            Id = $"{prefix}-6",
            Team = team,
            Number = 6,
            Name = "MF 6",
            Ability = 1,
            IsGoalkeeper = false,
            Position = new Position(2, 3)
        });
        pieces.Add(new Piece
        {
            Id = $"{prefix}-7",
            Team = team,
            Number = 7,
            Name = "MF 7",
            Ability = 2,
            IsGoalkeeper = false,
            Position = new Position(4, 3)
        });
        pieces.Add(new Piece
        {
            Id = $"{prefix}-8",
            Team = team,
            Number = 8,
            Name = "MF 8",
            Ability = 1,
            IsGoalkeeper = false,
            Position = new Position(6, 3)
        });

        // FW (能力2 × 1, 能力3 × 1(エース), 能力1 × 1)
        pieces.Add(new Piece
        {
            Id = $"{prefix}-9",
            Team = team,
            Number = 9,
            Name = "FW 9",
            Ability = 2,
            IsGoalkeeper = false,
            Position = new Position(3, 5)
        });
        pieces.Add(new Piece
        {
            Id = $"{prefix}-10",
            Team = team,
            Number = 10,
            Name = "FW 10 ★",
            Ability = 3, // エース
            IsGoalkeeper = false,
            Position = new Position(4, 5)
        });
        pieces.Add(new Piece
        {
            Id = $"{prefix}-11",
            Team = team,
            Number = 11,
            Name = "FW 11",
            Ability = 1,
            IsGoalkeeper = false,
            Position = new Position(5, 5)
        });

        return pieces;
    }
}
