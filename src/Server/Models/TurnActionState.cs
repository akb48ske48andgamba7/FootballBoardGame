namespace FootballBoardGame.Server.Models;

public class TurnActionState
{
    // 通常の移動は最大2名まで
    public const int MaxStandardMoveCount = 2;
    public HashSet<string> MovedPieceIds { get; set; } = new();

    // パスまたはシュートは1ターンに1回
    public bool HasPassedOrShot { get; set; }

    // GKの特権: GKがボールを保持しているターンに限り、GK自身を追加で1回移動可能
    public bool GkBonusAvailable { get; set; }
    public bool HasUsedGkBonusMove { get; set; }

    public int StandardMovesRemaining => Math.Max(0, MaxStandardMoveCount - MovedPieceIds.Count);

    public bool CanPieceMove(Piece piece)
    {
        // すでに通常の2名枠で動いている場合
        if (MovedPieceIds.Contains(piece.Id))
        {
            // GKであり、まだGKボーナス移動を使っていない場合は動ける
            if (piece.IsGoalkeeper && GkBonusAvailable && !HasUsedGkBonusMove)
            {
                return true;
            }
            return false;
        }

        // 通常の2名枠が残っていれば動ける
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
        if (MovedPieceIds.Contains(piece.Id))
        {
            // 2回目の移動（GKボーナス）
            if (piece.IsGoalkeeper && GkBonusAvailable && !HasUsedGkBonusMove)
            {
                HasUsedGkBonusMove = true;
            }
        }
        else
        {
            if (StandardMovesRemaining > 0)
            {
                MovedPieceIds.Add(piece.Id);
            }
            else if (piece.IsGoalkeeper && GkBonusAvailable && !HasUsedGkBonusMove)
            {
                HasUsedGkBonusMove = true;
            }
        }
    }
}
