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
            <h4 style={{ color: '#00d2ff', fontSize: '14px', marginBottom: '4px' }}>⚽ 基本仕様 & ピッチ</h4>
            <p>
              ・ピッチは縦10マス × 横5マス（左サイド/左ハーフ/センター/右ハーフ/右サイド）。<br />
              ・ゴールは縦両端のセンターレーンの外側に配置されています。<br />
              ・各チーム11名 ＋ ボール1個（能力3が1名、能力2が3名、能力1が7名。1名がGK）。
            </p>
          </div>

          <div>
            <h4 style={{ color: '#00ffaa', fontSize: '14px', marginBottom: '4px' }}>🏃 1ターンのアクション</h4>
            <p>
              1ターンの間に以下の2つを行えます（順序不問）：<br />
              1. <strong>移動</strong>: 最大2名まで、前後左右斜めに「最大2マス」移動（味方・相手のすり抜け可能）。ボール保持者の移動はドリブルになります。<br />
              2. <strong>パス / シュート</strong>: ボール保持者が縦横斜めの直線状に無制限距離で出せます。ゴール枠へ向かうものは「シュート」になります。
            </p>
          </div>

          <div>
            <h4 style={{ color: '#ffd700', fontSize: '14px', marginBottom: '4px' }}>🧤 GK（ゴールキーパー）の特権</h4>
            <p>
              GKがボールを持っているターンに限り、通常の2名移動・1回パスに加え、<strong>「GK自身を追加で移動」</strong>させることができます。
            </p>
          </div>

          <div>
            <h4 style={{ color: '#ff3366', fontSize: '14px', marginBottom: '4px' }}>🚩 オフサイド</h4>
            <p>
              パスが出た瞬間、受け手が「相手最後尾DF（GK除く）がいるマスの横ライン」より相手ゴール側にいた場合オフサイドとなり、相手ボールでターン交代となります（同ラインはセーフ）。ピッチ上に赤色のレーザーラインで常時可視化されています。
            </p>
          </div>

          <div>
            <h4 style={{ color: '#ffaa00', fontSize: '14px', marginBottom: '4px' }}>🎲 サイコロ勝負（デュエル）</h4>
            <p>
              ・<strong>タックル</strong>: 相手ボール保持マスに自分のコマを移動させた瞬間に発生。<br />
              ・<strong>パスカット/シュート阻止</strong>: パス/シュートの直線ルート上に相手がいる場合に発生。<br />
              ・<strong>判定</strong>: 「能力値の合計 ＋ サイコロの目（1〜6）」が大きいチームの勝利！
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
