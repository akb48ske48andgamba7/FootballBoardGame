namespace FootballBoardGame.Server.Models;

/// <summary>
/// 【学習用解説: 1ターンの行動回数と状態管理（State Pattern）】
/// 
/// サッカーボードゲームの「1ターンのルール」を集中管理するクラスです。
/// 
/// ■ なぜ GameState の中に直接変数を並べず、このクラスに分けたのか？
/// - 「単一責任の原則 (Single Responsibility Principle)」に基づいています。
/// - ターンごとにリセットされる「残り移動回数」「残りパスカウント」「GK特権」をひとまとめにしておくことで、
///   毎ターンの開始時に `CurrentTurnAction = new TurnActionState()` とするだけで、
///   バグを生むことなく綺麗に初期状態へリセットできます。
/// 
/// ■ ルール仕様:
/// - コマの移動: 最大3回まで（同じ選手が2回・3回動いても、別の選手が1回ずつ動いてもOK）
/// - パスまたはシュート: 最大2回まで
/// - GK特権: GKがボールをキャッチしたターンに限り、上記3回枠に加えてGK自身がもう1回追加移動可能
/// </summary>
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
