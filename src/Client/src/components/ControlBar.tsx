import React from 'react';
import type { GameState } from '../types/game';
import { Send, ArrowRight } from 'lucide-react';

interface ControlBarProps {
  state: GameState;
  isPassMode: boolean;
  onTogglePassMode: () => void;
  onEndTurn: () => void;
}

export const ControlBar: React.FC<ControlBarProps> = ({
  state,
  isPassMode,
  onTogglePassMode,
  onEndTurn,
}) => {
  const action = state.currentTurnAction;
  const remainingMoves = action?.standardMovesRemaining ?? 0;
  const hasPassed = action?.hasPassedOrShot ?? false;

  const currentTeamPieces = state.pieces.filter((p) => p.team === state.activeTeam);
  const teamHasBall = currentTeamPieces.some((p) => p.id === state.ball.holderPieceId);

  return (
    <div className="control-bar glass-panel">
      {/* 移動カウント表示 */}
      <div className="action-counters">
        <div className="counter-item">
          <span>移動残回数:</span>
          <div className="dots-container">
            <div className={`move-dot ${remainingMoves >= 1 ? 'active' : ''}`} />
            <div className={`move-dot ${remainingMoves >= 2 ? 'active' : ''}`} />
          </div>
          <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            ({remainingMoves}/2名)
          </span>
        </div>

        {/* GK特権バッジ */}
        {action?.gkBonusAvailable && (
          <div className="gk-badge-privilege">
            🧤 GKボーナス移動: {action.hasUsedGkBonusMove ? '済' : '可能'}
          </div>
        )}
      </div>

      {/* アクションボタン */}
      <div className="control-buttons">
        <button
          className={`btn-action btn-pass ${isPassMode ? 'active' : ''}`}
          disabled={!teamHasBall || hasPassed}
          onClick={onTogglePassMode}
          title={
            !teamHasBall
              ? 'ボール保持時のみパス/シュート可能'
              : hasPassed
              ? '今ターンはすでにパス/シュート実行済'
              : 'パス/シュートモード切替'
          }
        >
          <Send size={16} />
          <span>{isPassMode ? 'パスモード解除' : 'パス / シュート'}</span>
        </button>

        <button
          className="btn-action btn-end-turn"
          onClick={onEndTurn}
          disabled={state.pendingDuel !== null}
        >
          <span>ターン終了</span>
          <ArrowRight size={16} />
        </button>
      </div>
    </div>
  );
};
