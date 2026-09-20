import React, { useEffect } from 'react';
import type { GameState } from '../types/game';
import confetti from 'canvas-confetti';
import { Trophy, ArrowRight, RotateCcw } from 'lucide-react';

interface GoalModalProps {
  state: GameState;
  onNextHalf?: () => void;
  onRestart: () => void;
}

export const GoalModal: React.FC<GoalModalProps> = ({ state, onNextHalf, onRestart }) => {
  const isGameOver = state.phase === 'GameOver';
  const isHalfTime = state.phase === 'HalfTime';

  useEffect(() => {
    if (isGameOver) {
      confetti({
        particleCount: 120,
        spread: 100,
        origin: { y: 0.5 },
      });
    }
  }, [isGameOver]);

  if (!isGameOver && !isHalfTime) return null;

  const winner =
    state.scoreTeamA > state.scoreTeamB
      ? 'TEAM BLUE の勝利！🏆'
      : state.scoreTeamB > state.scoreTeamA
      ? 'TEAM RED の勝利！🏆'
      : '引き分け！🤝';

  return (
    <div className="modal-overlay">
      <div className="glass-panel setup-box" style={{ maxWidth: '520px' }}>
        {isGameOver ? (
          <>
            <Trophy size={56} color="#ffd700" style={{ margin: '0 auto 12px' }} />
            <h2 style={{ fontSize: '30px', fontWeight: 900, color: '#ffd700' }}>FULL TIME</h2>
            <div style={{ fontSize: '20px', fontWeight: 800, margin: '12px 0' }}>{winner}</div>

            <div
              style={{
                fontSize: '36px',
                fontWeight: 900,
                fontFamily: 'var(--font-mono)',
                margin: '16px 0',
              }}
            >
              <span style={{ color: 'var(--teamA-primary)' }}>{state.scoreTeamA}</span>
              <span style={{ margin: '0 16px', color: 'var(--text-muted)' }}>-</span>
              <span style={{ color: 'var(--teamB-primary)' }}>{state.scoreTeamB}</span>
            </div>

            <button
              className="btn-primary-action"
              onClick={onRestart}
              style={{ marginTop: '16px', width: '100%', justifyContent: 'center', display: 'flex', alignItems: 'center', gap: '8px' }}
            >
              <RotateCcw size={18} />
              <span>もう一度遊ぶ</span>
            </button>
          </>
        ) : (
          <>
            <h2 style={{ fontSize: '28px', fontWeight: 900, color: '#00d2ff' }}>HALF TIME</h2>
            <p style={{ color: 'var(--text-muted)', margin: '12px 0 20px', fontSize: '14px' }}>
              前半45ターンが終了しました。陣地を交代して後半戦へ突入します！
            </p>

            <div
              style={{
                fontSize: '32px',
                fontWeight: 800,
                fontFamily: 'var(--font-mono)',
                margin: '16px 0',
              }}
            >
              <span style={{ color: 'var(--teamA-primary)' }}>{state.scoreTeamA}</span>
              <span style={{ margin: '0 12px', color: 'var(--text-muted)' }}>-</span>
              <span style={{ color: 'var(--teamB-primary)' }}>{state.scoreTeamB}</span>
            </div>

            <button
              className="btn-primary-action"
              onClick={onNextHalf}
              style={{ marginTop: '16px', width: '100%', justifyContent: 'center', display: 'flex', alignItems: 'center', gap: '8px' }}
            >
              <span>後半の配置へ進む</span>
              <ArrowRight size={18} />
            </button>
          </>
        )}
      </div>
    </div>
  );
};
