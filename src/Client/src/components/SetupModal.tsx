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
  const isTopHalf = (isHalf1 && currentTeam === 'TeamA') || (!isHalf1 && currentTeam === 'TeamB');
  const allowedRows = isTopHalf ? 'Row 1〜5' : 'Row 6〜10';

  const teamName = currentTeam === 'TeamA' ? 'TEAM BLUE (チームA)' : 'TEAM RED (チームB)';

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
          自陣（縦5マス × 横5マス / <strong>{allowedRows}</strong>）に自由に11名を配置できます。
          <br />
          あらかじめバランス良く配置された「デフォルト・タクティクス（4-3-3）」を適用するか、配置を確定して進んでください。
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
