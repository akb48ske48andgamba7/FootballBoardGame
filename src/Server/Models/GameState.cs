namespace FootballBoardGame.Server.Models;

/// <summary>
/// 【学習用解説: ゲームの進行フェーズを表す列挙型 (Enum)】
/// 
/// ゲームの状態遷移（ステートマシン）を表現します。
/// 前半初期配置 → 前半試合 → ハーフタイム → 後半初期配置 → 後半試合 → 試合終了
/// と順番に遷移していきます。
/// </summary>
public enum GamePhase
{
    SetupFirstHalfA,    // 前半: TeamA 自陣配置 (Col 1〜6, Row 1〜7)
    SetupFirstHalfB,    // 前半: TeamB 自陣配置 (Col 7〜12, Row 1〜7)
    FirstHalf,          // 前半試合進行中 (1〜45ターン + AT)
    HalfTime,           // ハーフタイム (陣地交代)
    SetupSecondHalfA,   // 後半: TeamA 自陣配置 (Col 7〜12, Row 1〜7)
    SetupSecondHalfB,   // 後半: TeamB 自陣配置 (Col 1〜6, Row 1〜7)
    SecondHalf,         // 後半試合進行中 (1〜45ターン + AT)
    GameOver            // 試合終了 (フルタイム)
}

/// <summary>
/// 対戦モード: プレイヤー対戦 (PvP) または CPU対戦 (PvC)
/// </summary>
public enum GameMode
{
    PvP, // 2名対戦 (同じ端末で交互に操作)
    PvC  // 1名対戦 vs CPU (TeamBが自動思考AI)
}

/// <summary>
/// 【学習用解説: ゲーム全体の「唯一の真実の源 (Single Source of Truth)」】
/// 
/// バックエンドとフロントエンド（React）をつなぐ最重要クラスです。
/// 
/// ■ なぜこのような設計にするのか？
/// - Webアプリケーションでは、サーバー側でこの `GameState` オブジェクトを最新に保ち、
///   REST API を通じて JSON 形式でフロントエンド（ブラウザ）へ送信します。
/// - React 側はこの `GameState` をそのまま受け取って画面を描画するだけで済むため、
///   「サーバーと画面で状態の食い違いが起きない」堅牢な設計（ステート駆動）になります。
/// </summary>
public class GameState
{
    public Guid GameId { get; set; } = Guid.NewGuid();
    public GamePhase Phase { get; set; } = GamePhase.SetupFirstHalfA;
    public GameMode Mode { get; set; } = GameMode.PvC; // デフォルトでCPU対戦可能に
    public TeamType ActiveTeam { get; set; } = TeamType.TeamA; // 現在手番のチーム

    public int Half => (Phase == GamePhase.SetupFirstHalfA || Phase == GamePhase.SetupFirstHalfB || Phase == GamePhase.FirstHalf) ? 1 : 2;

    public int Turn { get; set; } = 1;                    // 1〜45
    public bool IsAdditionalTime { get; set; }
    public int AdditionalTimeTotal { get; set; }         // ダイス2個の合計 (2〜12)
    public int AdditionalTimeTurnsElapsed { get; set; }

    public int ScoreTeamA { get; set; }
    public int ScoreTeamB { get; set; }

    public List<Piece> Pieces { get; set; } = new();
    public Ball Ball { get; set; } = new();
    public TurnActionState CurrentTurnAction { get; set; } = new();

    public DuelContext? PendingDuel { get; set; }
    public DuelContext? LastResolvedDuel { get; set; }
    public List<string> MatchLogs { get; set; } = new();

    public string? OffsideWarning { get; set; }
}
