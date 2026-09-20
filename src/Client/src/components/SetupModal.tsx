import React from 'react';
import type { GameState, TeamType } from '../types/game';

interface SetupModalProps {
  state: GameState;
  onApplyDefault: (team: TeamType) => Promise<void>;
  onConfirmSetup: (team: TeamType) => Promise<void>;
}

export const SetupModal: React.FC<SetupModalProps> = ({
  state,
  onApplyDefault,
  onConfirmSetup,
}) => {
  const currentTeam: TeamType =
    state.phase === 'SetupFirstHalfA' || state.phase === 'SetupSecondHalfA'
      ? 'TeamA'
      : 'TeamB';

  const isHalf1 = state.half === 1;
  const isLeftHalf = (isHalf1 && currentTeam === 'TeamA') || (!isHalf1 && currentTeam === 'TeamB');
  const allowedCols = isLeftHalf ? 'Col 1〜5 (左陣)' : 'Col 6〜10 (右陣)';

  const isCpuTeam = state.mode === 'PvC' && currentTeam === 'TeamB';
  const teamName = currentTeam === 'TeamA' ? 'TEAM BLUE (あなた)' : isCpuTeam ? 'TEAM RED (🤖 CPU)' : 'TEAM RED (チームB)';

  return (
    <div className="modal-overlay">
      <div className="glass-panel setup-box">
        <h2 style={{ fontSize: '24px', fontWeight: 800, marginBottom: '6px' }}>
          ⚽ フォーメーション初期配置
        </h2>
        <div
          style={{
            fontSize: '16px',
            fontWeight: 700,
            color: currentTeam === 'TeamA' ? 'var(--teamA-primary)' : 'var(--teamB-primary)',
          }}
        >
          {isHalf1 ? '前半' : '後半'} : {teamName} の配置フェーズ
        </div>

        <p className="setup-instructions">
          自陣（縦7マス × 横5マス / <strong>{allowedCols}</strong>）に11名を配置します。
          <br />
          バランス良く配置された「デフォルト・タクティクス」を適用するか、配置を確定して進んでください。
        </p>

        <div className="setup-action-btns">
          <button
            className="btn-secondary"
            style={{ padding: '12px 24px', fontSize: '14px' }}
            onClick={() => onApplyDefault(currentTeam)}
          >
            📋 デフォルト配置を適用
          </button>

          <button
            className="btn-primary-action"
            onClick={() => onConfirmSetup(currentTeam)}
          >
            ✅ 配置を確定して次へ進む
          </button>
        </div>
      </div>
    </div>
  );
};
