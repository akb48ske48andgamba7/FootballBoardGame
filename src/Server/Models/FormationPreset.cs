namespace FootballBoardGame.Server.Models;

public record RelativePlacement(int Number, string PositionName, int Row, int RelativeCol, int DefaultAbility);

public class FormationPreset
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<RelativePlacement> Positions { get; set; } = new();

    public static readonly List<FormationPreset> All = new()
    {
        new FormationPreset
        {
            Id = "4-3-3",
            Name = "4-3-3 (ポゼッション/オランダ)",
            Category = "攻撃型",
            Description = "伝統のサイドアタック。現代バルセロナやマンチェスター・Cの代名詞。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RSB", 2, 2, 1),
                new(3, "RCB", 3, 2, 1),
                new(4, "LCB", 5, 2, 1),
                new(5, "LSB", 6, 2, 1),
                new(6, "DMF", 4, 3, 2),
                new(7, "RCM", 3, 4, 2),
                new(8, "LCM", 5, 4, 1),
                new(9, "RWG", 2, 6, 1),
                new(10, "CF", 4, 6, 3), // エース★3
                new(11, "LWG", 6, 6, 1),
            }
        },
        new FormationPreset
        {
            Id = "4-2-3-1",
            Name = "4-2-3-1 (ダブルボランチ)",
            Category = "バランス型",
            Description = "現代サッカーの黄金標準。2ボランチの安定感と2列目3人の攻撃力。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RSB", 2, 2, 1),
                new(3, "RCB", 3, 2, 1),
                new(4, "LCB", 5, 2, 1),
                new(5, "LSB", 6, 2, 1),
                new(6, "RDM", 3, 3, 1),
                new(7, "LDM", 5, 3, 2),
                new(8, "RAM", 2, 5, 1),
                new(9, "CAM", 4, 5, 2),
                new(10, "CF", 4, 6, 3), // エース★3
                new(11, "LAM", 6, 5, 1),
            }
        },
        new FormationPreset
        {
            Id = "4-4-2",
            Name = "4-4-2 (クラシック・フラット)",
            Category = "伝統型",
            Description = "イングランド伝統。2つの4人ブロックによる強固な守備とツートップの破壊力。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RSB", 2, 2, 1),
                new(3, "RCB", 3, 2, 1),
                new(4, "LCB", 5, 2, 1),
                new(5, "LSB", 6, 2, 1),
                new(6, "RM", 2, 4, 1),
                new(7, "RCM", 3, 4, 2),
                new(8, "LCM", 5, 4, 1),
                new(9, "LM", 6, 4, 1),
                new(10, "RST", 3, 6, 3), // エース★3
                new(11, "LST", 5, 6, 2),
            }
        },
        new FormationPreset
        {
            Id = "4-1-2-1-2",
            Name = "4-4-2 ダイヤモンド",
            Category = "ポゼッション型",
            Description = "ACミラン黄金期を支えたアンカー＋トップ下型。中盤の中央を圧倒的に支配。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RSB", 2, 2, 1),
                new(3, "RCB", 3, 2, 1),
                new(4, "LCB", 5, 2, 1),
                new(5, "LSB", 6, 2, 1),
                new(6, "CDM", 4, 3, 2),
                new(7, "RCM", 3, 4, 1),
                new(8, "LCM", 5, 4, 1),
                new(9, "CAM", 4, 5, 3), // エース司令塔★3
                new(10, "RST", 3, 6, 2),
                new(11, "LST", 5, 6, 1),
            }
        },
        new FormationPreset
        {
            Id = "3-5-2",
            Name = "3-5-2 (ウイングバック)",
            Category = "可変・守備型",
            Description = "イタリア伝統のカテナチオ進化系。ウイングバックがピッチ全体を上下動。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RCB", 3, 2, 1),
                new(3, "CB", 4, 2, 2),
                new(4, "LCB", 5, 2, 1),
                new(5, "RWB", 1, 4, 1),
                new(6, "LWB", 7, 4, 1),
                new(7, "RCM", 3, 4, 1),
                new(8, "DM", 4, 3, 1),
                new(9, "LCM", 5, 4, 2),
                new(10, "RST", 3, 6, 3), // エース★3
                new(11, "LST", 5, 6, 1),
            }
        },
        new FormationPreset
        {
            Id = "3-4-2-1",
            Name = "3-4-2-1 (シャドーツートップ)",
            Category = "戦術型",
            Description = "日本代表や欧州強豪が採用する可変戦術。2シャドーが相手バイタルエリアを攻略。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RCB", 3, 2, 1),
                new(3, "CB", 4, 2, 2),
                new(4, "LCB", 5, 2, 1),
                new(5, "RWB", 1, 4, 1),
                new(6, "LWB", 7, 4, 1),
                new(7, "RDM", 3, 3, 1),
                new(8, "LDM", 5, 3, 1),
                new(9, "RSH", 3, 5, 2),
                new(10, "CF", 4, 6, 3), // エース★3
                new(11, "LSH", 5, 5, 1),
            }
        },
        new FormationPreset
        {
            Id = "3-4-3",
            Name = "3-4-3 (超攻撃的サイド)",
            Category = "攻撃型",
            Description = "前線3枚と中盤の両翼で相手サイドを強襲するダイナミックなアタッキング布陣。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RCB", 3, 2, 1),
                new(3, "CB", 4, 2, 2),
                new(4, "LCB", 5, 2, 1),
                new(5, "RM", 1, 4, 1),
                new(6, "RCM", 3, 4, 1),
                new(7, "LCM", 5, 4, 2),
                new(8, "LM", 7, 4, 1),
                new(9, "RW", 2, 6, 1),
                new(10, "CF", 4, 6, 3), // エース★3
                new(11, "LW", 6, 6, 1),
            }
        },
        new FormationPreset
        {
            Id = "4-1-4-1",
            Name = "4-1-4-1 (アンカーシステム)",
            Category = "ポゼッション型",
            Description = "守備専門のアンカーがDFライン前をプロテクトし、前線4枚が波状攻撃を仕掛ける。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RSB", 2, 2, 1),
                new(3, "RCB", 3, 2, 1),
                new(4, "LCB", 5, 2, 1),
                new(5, "LSB", 6, 2, 1),
                new(6, "CDM", 4, 3, 2),
                new(7, "RM", 2, 5, 1),
                new(8, "RCM", 3, 5, 2),
                new(9, "LCM", 5, 5, 1),
                new(10, "CF", 4, 6, 3), // エース★3
                new(11, "LM", 6, 5, 1),
            }
        },
        new FormationPreset
        {
            Id = "4-3-1-2",
            Name = "4-3-1-2 (ファンタジスタ型)",
            Category = "中央突破型",
            Description = "3枚の中盤フラットの前に置かれたトップ下の天才がツートップへ絶妙ラストパス。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RSB", 2, 2, 1),
                new(3, "RCB", 3, 2, 1),
                new(4, "LCB", 5, 2, 1),
                new(5, "LSB", 6, 2, 1),
                new(6, "RCM", 2, 3, 1),
                new(7, "CM", 4, 3, 2),
                new(8, "LCM", 6, 3, 1),
                new(9, "CAM", 4, 5, 3), // エース司令塔★3
                new(10, "RST", 3, 6, 2),
                new(11, "LST", 5, 6, 1),
            }
        },
        new FormationPreset
        {
            Id = "4-2-2-2",
            Name = "4-2-2-2 (マジックスクエア)",
            Category = "ブラジル型",
            Description = "ブラジル黄金期を象徴する陣形。2ボランチと2トップ下が四角形を形成し変幻自在。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RSB", 2, 2, 1),
                new(3, "RCB", 3, 2, 1),
                new(4, "LCB", 5, 2, 1),
                new(5, "LSB", 6, 2, 1),
                new(6, "RDM", 3, 3, 1),
                new(7, "LDM", 5, 3, 2),
                new(8, "RAM", 3, 5, 2),
                new(9, "LAM", 5, 5, 1),
                new(10, "RST", 3, 6, 3), // エース★3
                new(11, "LST", 5, 6, 1),
            }
        },
        new FormationPreset
        {
            Id = "3-4-1-2",
            Name = "3-4-1-2 (3バック＋トップ下)",
            Category = "カウンター型",
            Description = "3バックと両翼でピッチを広く守り、トップ下からツートップへ電光石火の縦パス。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RCB", 3, 2, 1),
                new(3, "CB", 4, 2, 2),
                new(4, "LCB", 5, 2, 1),
                new(5, "RWB", 1, 4, 1),
                new(6, "LWB", 7, 4, 1),
                new(7, "RCM", 3, 3, 1),
                new(8, "LCM", 5, 3, 1),
                new(9, "CAM", 4, 5, 3), // エース★3
                new(10, "RST", 3, 6, 2),
                new(11, "LST", 5, 6, 1),
            }
        },
        new FormationPreset
        {
            Id = "5-3-2",
            Name = "5-3-2 (堅守カウンター)",
            Category = "守備型",
            Description = "5バックの堅牢な堤防で自陣を完全防御し、前線のツートップへロングフィード。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RWB", 1, 2, 1),
                new(3, "RCB", 3, 2, 1),
                new(4, "CB", 4, 2, 2),
                new(5, "LCB", 5, 2, 1),
                new(6, "LWB", 7, 2, 1),
                new(7, "RCM", 3, 4, 1),
                new(8, "CM", 4, 4, 2),
                new(9, "LCM", 5, 4, 1),
                new(10, "RST", 3, 6, 3), // エース★3
                new(11, "LST", 5, 6, 1),
            }
        },
        new FormationPreset
        {
            Id = "5-4-1",
            Name = "5-4-1 (ソリッドブロック)",
            Category = "超守備型",
            Description = "ペナルティエリア前のスペースを隙間なく埋める超堅牢な要塞型フォーメーション。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RWB", 1, 2, 1),
                new(3, "RCB", 3, 2, 1),
                new(4, "CB", 4, 2, 2),
                new(5, "LCB", 5, 2, 1),
                new(6, "LWB", 7, 2, 1),
                new(7, "RM", 2, 4, 1),
                new(8, "RCM", 3, 4, 1),
                new(9, "LCM", 5, 4, 2),
                new(10, "CF", 4, 6, 3), // エース★3
                new(11, "LM", 6, 4, 1),
            }
        },
        new FormationPreset
        {
            Id = "4-5-1",
            Name = "4-5-1 (中盤フラット厚み)",
            Category = "中盤支配型",
            Description = "中盤5枚の分厚いフィルターで敵のパスコースを遮断し、セカンドボールを拾い捲る。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RSB", 2, 2, 1),
                new(3, "RCB", 3, 2, 1),
                new(4, "LCB", 5, 2, 1),
                new(5, "LSB", 6, 2, 1),
                new(6, "RM", 1, 4, 1),
                new(7, "RCM", 3, 4, 1),
                new(8, "CM", 4, 4, 2),
                new(9, "LCM", 5, 4, 2),
                new(10, "CF", 4, 6, 3), // エース★3
                new(11, "LM", 7, 4, 1),
            }
        },
        new FormationPreset
        {
            Id = "3-3-3-1",
            Name = "3-3-3-1 (ビエルサ式ハイプレス)",
            Category = "超プレッシング型",
            Description = "マルセロ・ビエルサの代名詞。縦3列のユニットが連動して前線から猛烈にボール狩り。",
            Positions = new()
            {
                new(1, "GK", 4, 1, 2),
                new(2, "RCB", 3, 2, 1),
                new(3, "CB", 4, 2, 2),
                new(4, "LCB", 5, 2, 1),
                new(5, "RDM", 2, 3, 1),
                new(6, "DM", 4, 3, 1),
                new(7, "LDM", 6, 3, 2),
                new(8, "RAM", 2, 5, 1),
                new(9, "CAM", 4, 5, 2),
                new(10, "CF", 4, 6, 3), // エース★3
                new(11, "LAM", 6, 5, 1),
            }
        }
    };
}
