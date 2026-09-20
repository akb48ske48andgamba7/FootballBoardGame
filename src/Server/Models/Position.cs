namespace FootballBoardGame.Server.Models;

public record Position(int Row, int Col)
{
    // ピッチ内部: 縦7分割 (Row 1〜7), 横12マス (Col 1〜12)
    // 縦7レーン: 1:上サイド, 2:上ハーフ, 3:上インサイド, 4:センター, 5:下インサイド, 6:下ハーフ, 7:下サイド
    public bool IsInsidePitch => Row >= 1 && Row <= 7 && Col >= 1 && Col <= 12;

    // TeamAゴール: Col 0, Row 4 (左側ゴール / TeamBが攻める)
    // TeamBゴール: Col 13, Row 4 (右側ゴール / TeamAが攻める)
    public static readonly Position GoalA = new(4, 0);
    public static readonly Position GoalB = new(4, 13);

    public bool IsGoalForTeam(TeamType team, int half = 1)
    {
        // 前半: TeamAはCol 13 (GoalB / 右) へ攻める、TeamBはCol 0 (GoalA / 左) へ攻める
        // 後半: 陣地交代するため、TeamAはCol 0 (GoalA / 左) へ攻める、TeamBはCol 13 (GoalB / 右) へ攻める
        Position targetGoal = GetTargetGoal(team, half);
        return Row == targetGoal.Row && Col == targetGoal.Col;
    }

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

    // ペナルティーエリア判定 (センターと上下のインサイド Row 3〜5、ゴールに最も近い列 Col 1 または Col 12)
    public bool IsInPenaltyArea(TeamType defendingTeam, int half)
    {
        Position ownGoal = GetOwnGoal(defendingTeam, half);
        int paCol = ownGoal.Col == 0 ? 1 : 12;
        return Col == paCol && (Row >= 3 && Row <= 5);
    }

    // チェビシェフ距離 (縦・横・斜めを1歩として何歩で到達できるか)
    public int ChebyshevDistance(Position other) =>
        Math.Max(Math.Abs(Row - other.Row), Math.Abs(Col - other.Col));

    // 縦・横・斜めの直線かどうかの判定
    public bool IsStraightLineTo(Position other)
    {
        int dRow = Math.Abs(Row - other.Row);
        int dCol = Math.Abs(Col - other.Col);
        return dRow == 0 || dCol == 0 || dRow == dCol;
    }

    // 始点から終点までの直線上の中間マスリスト (始点・終点は含まない)
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
