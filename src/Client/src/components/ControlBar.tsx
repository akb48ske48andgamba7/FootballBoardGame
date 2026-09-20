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
  const remainingPasses = action?.remainingPassOrShots ?? 0;

  const currentTeamPieces = state.pieces.filter((p) => p.team === state.activeTeam);
  const teamHasBall = currentTeamPieces.some((p) => p.id === state.ball.holderPieceId);

  return (
    <div className="control-bar glass-panel">
      {/* 移動 & パスカウント表示 */}
      <div className="action-counters">
        <div className="counter-item">
          <span>🏃 移動残回数:</span>
          <div className="dots-container">
            <div className={`move-dot ${remainingMoves >= 1 ? 'active' : ''}`} />
            <div className={`move-dot ${remainingMoves >= 2 ? 'active' : ''}`} />
            <div className={`move-dot ${remainingMoves >= 3 ? 'active' : ''}`} />
          </div>
          <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            ({remainingMoves}/3回)
          </span>
        </div>

        <div className="counter-item">
          <span>🎯 パス/シュート:</span>
          <div className="dots-container">
            <div className={`pass-dot ${remainingPasses >= 1 ? 'active' : ''}`} />
            <div className={`pass-dot ${remainingPasses >= 2 ? 'active' : ''}`} />
          </div>
          <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            ({remainingPasses}/2回)
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
          disabled={!teamHasBall || remainingPasses <= 0}
          onClick={onTogglePassMode}
          title={
            !teamHasBall
              ? 'ボール保持時のみパス/シュート可能'
              : remainingPasses <= 0
              ? '今ターンはすでにパス/シュート制限(2回)に到達'
              : 'パス/シュートモード切替'
          }
        >
          <Send size={16} />
          <span>{isPassMode ? 'パスモード解除' : `パス / シュート (${remainingPasses})`}</span>
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
