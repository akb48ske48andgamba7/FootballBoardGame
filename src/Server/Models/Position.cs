namespace FootballBoardGame.Server.Models;

public record Position(int Row, int Col)
{
    // ピッチ内部: Row 1〜10, Col 1〜5
    // 横5レーン: 1:左サイド, 2:左ハーフ, 3:センター, 4:右ハーフ, 5:右サイド
    public bool IsInsidePitch => Row >= 1 && Row <= 10 && Col >= 1 && Col <= 5;

    // TeamAゴール: Row 0, Col 3 (TeamBが攻める)
    // TeamBゴール: Row 11, Col 3 (TeamAが攻める)
    public static readonly Position GoalA = new(0, 3);
    public static readonly Position GoalB = new(11, 3);

    public bool IsGoalForTeam(TeamType team, int half = 1)
    {
        // 前半: TeamAはRow 11 (GoalB) へ攻める、TeamBはRow 0 (GoalA) へ攻める
        // 後半: 陣地交代するため、TeamAはRow 0 (GoalA) へ攻める、TeamBはRow 11 (GoalB) へ攻める
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
