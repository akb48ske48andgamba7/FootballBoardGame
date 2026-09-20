namespace FootballBoardGame.Server.Models;

/// <summary>
/// 【学習用解説: C#の「レコード型 (record)」と幾何計算】
/// 
/// 1. なぜ class ではなく record なのか？
///    - record は「値の等価性 (Value Equality)」を自動で持ちます。
///      例えば `new Position(4, 2) == new Position(4, 2)` は、別インスタンスでも true になります。
///      盤面のマス目を表す座標データは「値そのもの」が重要であるため、不変な record が最適です。
/// 
/// 2. ピッチの座標系:
///    - 縦方向 (Row): 1〜7 (1:上サイド, 2:上ハーフ, 3:上インサイド, 4:センター, 5:下インサイド, 6:下ハーフ, 7:下サイド)
///    - 横方向 (Col): 1〜12 (左側ゴール: Col 0, 右側ゴール: Col 13)
/// </summary>
public record Position(int Row, int Col)
{
    // ピッチ内部: 縦7分割 (Row 1〜7), 横12マス (Col 1〜12)
    public bool IsInsidePitch => Row >= 1 && Row <= 7 && Col >= 1 && Col <= 12;

    // TeamAゴール: Col 0, Row 4 (左側ゴール / TeamBが攻める)
    // TeamBゴール: Col 13, Row 4 (右側ゴール / TeamAが攻める)
    public static readonly Position GoalA = new(4, 0);
    public static readonly Position GoalB = new(4, 13);

    /// <summary>
    /// 指定されたチームにとって、この座標が「相手のゴール」であるかを判定します。
    /// サッカー同様、ハーフタイムで陣地が交代（前半と後半で攻める向きが逆）になります。
    /// </summary>
    public bool IsGoalForTeam(TeamType team, int half = 1)
    {
        Position targetGoal = GetTargetGoal(team, half);
        return Row == targetGoal.Row && Col == targetGoal.Col;
    }

    /// <summary>
    /// 各チームが現在攻めるべき相手ゴール座標を取得します。
    /// 前半: TeamAは右 (Col 13), TeamBは左 (Col 0)
    /// 後半: 陣地交代により TeamAは左 (Col 0), TeamBは右 (Col 13)
    /// </summary>
    public static Position GetTargetGoal(TeamType team, int half)
    {
        if (half == 1)
        {
            return team == TeamType.TeamA ? GoalB : GoalA;
        }
        else
        {
            return team == TeamType.TeamA ? GoalA : GoalB;
        }
    }

    /// <summary>
    /// 自チームが守るべき自陣のゴール座標を取得します。
    /// </summary>
    public static Position GetOwnGoal(TeamType team, int half)
    {
        if (half == 1)
        {
            return team == TeamType.TeamA ? GoalA : GoalB;
        }
        else
        {
            return team == TeamType.TeamA ? GoalB : GoalA;
        }
    }

    /// <summary>
    /// ペナルティーエリア判定:
    /// ゴールキーパーが手を使って守れる神聖なエリア。
    /// センターおよび上下インサイド（Row 3〜5）で、ゴール側の横2列分（Col 1〜2 または Col 11〜12）の計6マス。
    /// </summary>
    public bool IsInPenaltyArea(TeamType defendingTeam, int half)
    {
        Position ownGoal = GetOwnGoal(defendingTeam, half);
        bool isLeftGoal = ownGoal.Col == 0;
        bool inColRange = isLeftGoal ? (Col == 1 || Col == 2) : (Col == 11 || Col == 12);
        return inColRange && (Row >= 3 && Row <= 5);
    }

    /// <summary>
    /// 【アルゴリズム解説: チェビシェフ距離 (Chebyshev Distance)】
    /// 将棋の王将やチェスのキングのように、「縦・横・斜めの8方向すべてを1歩で移動できる」グリッド盤面での最短距離を計算します。
    /// 数式: max(|Row1 - Row2|, |Col1 - Col2|)
    /// 例: (4, 4) から (2, 2) への移動は、縦2マスかつ横2マス（斜め2歩）なので距離は max(2, 2) = 2 となります。
    /// </summary>
    public int ChebyshevDistance(Position other) =>
        Math.Max(Math.Abs(Row - other.Row), Math.Abs(Col - other.Col));

    /// <summary>
    /// 2点間が縦・横・斜めの完全な「直線」上にあるかを判定します。
    /// パスやシュートは直線方向にのみ通すことができます。
    /// - 縦方向の直線: dCol == 0
    /// - 横方向の直線: dRow == 0
    /// - 斜め方向の直線: dRow == dCol
    /// </summary>
    public bool IsStraightLineTo(Position other)
    {
        int dRow = Math.Abs(Row - other.Row);
        int dCol = Math.Abs(Col - other.Col);
        return dRow == 0 || dCol == 0 || dRow == dCol;
    }

    /// <summary>
    /// 【アルゴリズム解説: レイキャスティング / 直線上の遮蔽物判定】
    /// 始点から終点までの直線上にあるすべての中間マスを順番に返します（始点・終点は除く）。
    /// パスコースやシュートコース上に相手ディフェンス選手が立ちふさがっているかを判定し、
    /// 「インターセプト（パスカット）勝負」を発生させるために使用されます。
    /// </summary>
    public List<Position> GetStraightPathTo(Position destination)
    {
        var path = new List<Position>();
        if (!IsStraightLineTo(destination)) return path;

        int stepRow = Math.Sign(destination.Row - Row);
        int stepCol = Math.Sign(destination.Col - Col);

        int curRow = Row + stepRow;
        int curCol = Col + stepCol;

        while (curRow != destination.Row || curCol != destination.Col)
        {
            path.Add(new Position(curRow, curCol));
            curRow += stepRow;
            curCol += stepCol;
        }

        return path;
    }
}
