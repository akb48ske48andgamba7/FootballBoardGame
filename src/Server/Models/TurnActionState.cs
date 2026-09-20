namespace FootballBoardGame.Server.Models;

public class TurnActionState
{
    // 通常の移動は最大3回まで (同一選手の複数回移動も可能)
    public const int MaxStandardMoveCount = 3;
    public int StandardMoveCount { get; set; }
    public HashSet<string> MovedPieceIds { get; set; } = new();

    // パスまたはシュートは最大2回まで
    public const int MaxPassOrShotCount = 2;
    public int PassOrShotCount { get; set; }

    public bool HasPassedOrShot => PassOrShotCount >= MaxPassOrShotCount;
    public int RemainingPassOrShots => Math.Max(0, MaxPassOrShotCount - PassOrShotCount);
    public bool CanPassOrShot => RemainingPassOrShots > 0;

    // GKの特権: GKがボールを保持しているターンに限り、GK自身を追加で1回移動可能
    public bool GkBonusAvailable { get; set; }
    public bool HasUsedGkBonusMove { get; set; }

    public int StandardMovesRemaining => Math.Max(0, MaxStandardMoveCount - StandardMoveCount);

    public bool CanPieceMove(Piece piece)
    {
        // 通常の3回枠が残っていれば同一選手でも動ける
        if (StandardMovesRemaining > 0)
        {
            return true;
        }

        // 通常枠は埋まっているが、GKであり、GKボーナス移動が残っている場合
        if (piece.IsGoalkeeper && GkBonusAvailable && !HasUsedGkBonusMove)
        {
            return true;
        }

        return false;
    }

    public void RecordMove(Piece piece)
    {
        if (StandardMovesRemaining > 0)
        {
            StandardMoveCount++;
            MovedPieceIds.Add(piece.Id);
        }
        else if (piece.IsGoalkeeper && GkBonusAvailable && !HasUsedGkBonusMove)
        {
            HasUsedGkBonusMove = true;
            MovedPieceIds.Add(piece.Id);
        }
    }

    public void RecordPassOrShot()
    {
        PassOrShotCount++;
    }
}
