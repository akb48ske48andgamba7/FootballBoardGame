import React from 'react';
import { X } from 'lucide-react';

interface RuleGuideModalProps {
  onClose: () => void;
}

export const RuleGuideModal: React.FC<RuleGuideModalProps> = ({ onClose }) => {
  return (
    <div className="modal-overlay" onClick={onClose}>
      <div
        className="glass-panel setup-box"
        style={{ maxWidth: '640px', textAlign: 'left', maxHeight: '85vh', overflowY: 'auto' }}
        onClick={(e) => e.stopPropagation()}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h2 style={{ fontSize: '20px', fontWeight: 800 }}>📖 ゲームルール＆仕様ガイド</h2>
          <button className="btn-secondary" style={{ padding: '6px' }} onClick={onClose}>
            <X size={18} />
          </button>
        </div>

        <div style={{ fontSize: '13px', lineHeight: 1.7, color: 'var(--text-main)', display: 'flex', flexDirection: 'column', gap: '14px' }}>
          <div>
            <h4 style={{ color: '#00d2ff', fontSize: '14px', marginBottom: '4px' }}>⚽ 横向きピッチ (縦7分割 × 横12マス)</h4>
            <p>
              ・ピッチは横12マス（進行方向 Col 1〜12）× 縦7分割レーン（上サイド〜センター〜下サイド）。<br />
              ・ゴールはピッチ左右両端（Col 0 および Col 13）の中央レーン（Row 4）外側に配置。<br />
              ・各チーム11名 ＋ ボール1個（能力3×1名、能力2×3名、能力1×7名。1名がGK）。
            </p>
          </div>

          <div>
            <h4 style={{ color: '#ff7300', fontSize: '14px', marginBottom: '4px' }}>🤖 CPU対戦 & 2人対戦</h4>
            <p>
              ・ヘッダーの「vs CPU」を選ぶと、1人でCPU（Team RED）と本格タクティクス対戦が可能です。<br />
              ・「2人対戦」に切り替えると、1台の端末で2名のプレイヤーが交互に手番を指せます。
            </p>
          </div>

          <div>
            <h4 style={{ color: '#00ffaa', fontSize: '14px', marginBottom: '4px' }}>🏃 1ターンのアクション (順序自由)</h4>
            <p>
              1. <strong>移動</strong>: 最大3名まで、前後左右斜めに「最大2マス」移動（味方・相手のすり抜け可能。同一選手の複数回移動も可能）。ボール保持者の移動はドリブルになります。<br />
              2. <strong>パス / シュート</strong>: 1ターンに最大2回まで、縦横斜めの直線状に<strong>「最大6マス以内」</strong>で出せます。相手ゴール枠へ向かう直線パスは「シュート」になります。
            </p>
          </div>

          <div>
            <h4 style={{ color: '#ffd700', fontSize: '14px', marginBottom: '4px' }}>🧤 GK（ゴールキーパー）とペナルティエリア</h4>
            <p>
              ・<strong>ペナルティエリア内セーブ保証</strong>: センターと上下インサイドのゴール手前2列（Row 3〜5, Col 1〜2 または 11〜12）にGKがいる際、ゴールへのシュートに対して直線上にいなくても必ずGKとの勝負が発生します。<br />
              ・<strong>GK手を使ったセーブ (能力+1)</strong>: ゴールに向かうシュートが打たれたときのみ、守備側GKは手を使って守れるルールとして能力値が「+1」されます。<br />
              ・<strong>シュートコース上のDFによるGK能力加算 (+1/名)</strong>: シュートコース上に相手ディフェンダーがいた場合、ディフェンダーの人数分だけGKの能力が「+1」加算（コース限定・壁補正）されます！<br />
              ・<strong>GKボール保持時のタックル禁止 (GK保護)</strong>: ゴールキーパーがボールを保持している間は、相手選手はGKにタックルに行くことができません。<br />
              ・<strong>GKボーナス移動</strong>: GKがボールを保持しているターンに限り、追加でGK自身を1回移動できます。
            </p>
          </div>

          <div>
            <h4 style={{ color: '#ff3366', fontSize: '14px', marginBottom: '4px' }}>🚩 オフサイド</h4>
            <p>
              パスが出た瞬間、受け手が「相手最後尾DF（GK除く）がいる縦ライン（Col）」より相手ゴール側にいた場合オフサイド。ピッチ上に赤色レーザー光線でリアルタイムに縦オフサイドラインが表示されます。
            </p>
          </div>

          <div>
            <h4 style={{ color: '#ffaa00', fontSize: '14px', marginBottom: '4px' }}>🎲 サイコロ勝負（デュエル）</h4>
            <p>
              ・<strong>タックル</strong>: 相手ボール保持マスに自分のコマを移動させた瞬間に発生（<strong>1選手につき1ターン1回まで</strong>）。<br />
              ・<strong>パスカット/シュート阻止</strong>: パス/シュートの直線経路上に相手がいる場合に発生。<br />
              ・<strong>判定</strong>: 「能力値合計 ＋ サイコロの目（1〜6）」が大きいチームの勝利！合計値メーターに出目がリアルタイム加算されます。<br />
              ・<strong>左右配置</strong>: 仕掛けた側にかかわらず、ピッチ自陣と同じ左右配置（前半: 左Blue/右Red, 後半: 左Red/右Blue）で直感的に勝負を確認できます。
            </p>
          </div>

          <div>
            <h4 style={{ color: '#c0d4f5', fontSize: '14px', marginBottom: '4px' }}>⏱️ アディショナルタイム & 勝敗</h4>
            <p>
              前半45ターン、後半45ターン終了時、サイコロを2個振った合計値（2〜12ターン）がアディショナルタイムとして加算されます。
            </p>
          </div>
        </div>

        <button
          className="btn-primary-action"
          style={{ width: '100%', marginTop: '20px' }}
          onClick={onClose}
        >
          閉じる
        </button>
      </div>
    </div>
  );
};
