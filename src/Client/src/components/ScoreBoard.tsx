import React from 'react';
import type { GameState } from '../types/game';

interface ScoreBoardProps {
  state: GameState;
  isCpuThinking?: boolean;
}

export const ScoreBoard: React.FC<ScoreBoardProps> = ({ state, isCpuThinking }) => {
  const isFirstHalf = state.half === 1;
  const isAdditional = state.isAdditionalTime;
  const isCpuMode = state.mode === 'PvC';

  return (
    <div className="scoreboard-container glass-panel">
      {/* チームA スコアカード */}
      <div className="team-score-card team-a">
        <div className="team-badge-circle">A</div>
        <div className="team-info">
          <span className="team-name">
            TEAM BLUE {isCpuMode ? '(あなた)' : ''}
          </span>
          <span className="team-score">{state.scoreTeamA}</span>
        </div>
      </div>

      {/* 試合ステータス (ターン / 前半後半 / 手番) */}
      <div className="match-status-center">
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span className="turn-pill">
            {isFirstHalf ? '1ST HALF' : '2ND HALF'} : Turn {state.turn} / 45
          </span>
          {isAdditional && (
            <span className="additional-time-badge">
              +AT {state.additionalTimeTurnsElapsed} / {state.additionalTimeTotal}
            </span>
          )}
        </div>

        {/* 現在の手番表示 */}
        {isCpuThinking ? (
          <div className="cpu-thinking-pill">
            <span>🤖 CPU思考中...</span>
          </div>
        ) : (
          <div
            className={`active-turn-indicator ${
              state.activeTeam === 'TeamA' ? 'turn-A' : 'turn-B'
            }`}
          >
            <span style={{ fontSize: '10px' }}>●</span>
            <span>
              {state.activeTeam === 'TeamA'
                ? 'TEAM BLUE のターン'
                : isCpuMode
                ? 'TEAM RED (CPU) のターン'
                : 'TEAM RED のターン'}
            </span>
          </div>
        )}

        {state.offsideWarning && (
          <span style={{ fontSize: '11px', color: '#ff3366', fontWeight: 600 }}>
            ⚠️ {state.offsideWarning}
          </span>
        )}
      </div>

      {/* チームB スコアカード */}
      <div className="team-score-card team-b" style={{ justifyContent: 'flex-end' }}>
        <div className="team-info" style={{ textAlign: 'right' }}>
          <span className="team-name">
            TEAM RED {isCpuMode ? '(🤖 CPU)' : ''}
          </span>
          <span className="team-score">{state.scoreTeamB}</span>
        </div>
        <div className="team-badge-circle">B</div>
      </div>
    </div>
  );
};
