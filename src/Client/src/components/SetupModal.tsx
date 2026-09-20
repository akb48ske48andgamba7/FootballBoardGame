import React, { useState } from 'react';
import type { GameState, PiecePlacementDto, TeamType } from '../types/game';
import { FORMATION_PRESETS } from '../data/formations';
import { Sparkles, Shield, Compass } from 'lucide-react';

interface SetupModalProps {
  state: GameState;
  onConfirmSetup: (team: TeamType, placements: PiecePlacementDto[]) => Promise<void>;
}

export const SetupModal: React.FC<SetupModalProps> = ({
  state,
  onConfirmSetup,
}) => {
  const currentTeam: TeamType =
    state.phase === 'SetupFirstHalfA' || state.phase === 'SetupSecondHalfA'
      ? 'TeamA'
      : 'TeamB';

  const isHalf1 = state.half === 1;
  const isLeftHalf = (isHalf1 && currentTeam === 'TeamA') || (!isHalf1 && currentTeam === 'TeamB');
  const teamName =
    currentTeam === 'TeamA'
      ? 'TEAM BLUE (あなた)'
      : state.mode === 'PvC'
      ? 'TEAM RED (🤖 CPU)'
      : 'TEAM RED (チームB)';

  // 選択中のフォーメーション (デフォルト: 4-3-3)
  const [selectedFormationId, setSelectedFormationId] = useState<string>('4-3-3');

  // 現在のフォーメーション定義を取得
  const currentPreset =
    FORMATION_PRESETS.find((f) => f.id === selectedFormationId) ?? FORMATION_PRESETS[0];

  // 各コマ（1〜11番）の能力値マップ (デフォルトはプリセット定義に従う)
  const [abilities, setAbilities] = useState<Record<number, number>>(() => {
    const initial: Record<number, number> = {};
    for (const pos of currentPreset.positions) {
      initial[pos.number] = pos.defaultAbility;
    }
    return initial;
  });

  // フォーメーションを切り替えたとき
  const handleSelectFormation = (fId: string) => {
    setSelectedFormationId(fId);
    const preset = FORMATION_PRESETS.find((f) => f.id === fId) ?? FORMATION_PRESETS[0];
    const newAbilities: Record<number, number> = {};
    for (const pos of preset.positions) {
      newAbilities[pos.number] = pos.defaultAbility;
    }
    setAbilities(newAbilities);
  };

  // 能力値の切り替え (★1 → ★2 → ★3 → ★1)
  const handleCycleAbility = (number: number) => {
    setAbilities((prev) => {
      const current = prev[number] || 1;
      const next = current === 1 ? 2 : current === 2 ? 3 : 1;
      return { ...prev, [number]: next };
    });
  };

  // 直接能力値をセット
  const handleSetAbility = (number: number, ability: number) => {
    setAbilities((prev) => ({ ...prev, [number]: ability }));
  };

  // クイックプリセット適用
  const applyQuickPreset = (type: 'fw' | 'mf' | 'gk') => {
    const nextAbilities: Record<number, number> = {};
    // まず全員★1
    for (let i = 1; i <= 11; i++) {
      nextAbilities[i] = 1;
    }

    if (type === 'fw') {
      // FWエース型: #10(CF)が★3, #1(GK), #9, #7が★2
      nextAbilities[10] = 3;
      nextAbilities[1] = 2; // GK
      nextAbilities[9] = 2;
      nextAbilities[7] = 2;
    } else if (type === 'mf') {
      // 司令塔MF型: #7(または#9)が★3, #10(FW), #1(GK), #6(DMF)が★2
      const camOrCm = currentPreset.positions.find((p) => p.positionName.includes('AM') || p.positionName.includes('CM'))?.number || 7;
      nextAbilities[camOrCm] = 3;
      nextAbilities[1] = 2; // GK
      nextAbilities[10] = 2; // FW
      nextAbilities[6] = 2; // MF
    } else if (type === 'gk') {
      // 守護神GK型: #1(GK)が★3, #10(FW), #3(CB), #7(MF)が★2
      nextAbilities[1] = 3; // GKエース守護神
      nextAbilities[10] = 2; // FW
      nextAbilities[3] = 2; // CB
      nextAbilities[7] = 2; // MF
    }

    setAbilities(nextAbilities);
  };

  // 能力値の配分カウント
  const count3 = Object.values(abilities).filter((a) => a === 3).length;
  const count2 = Object.values(abilities).filter((a) => a === 2).length;
  const count1 = Object.values(abilities).filter((a) => a === 1).length;

  const isValidAllocation = count3 === 1 && count2 === 3 && count1 === 7;

  // 配置確定
  const handleConfirm = async () => {
    if (!isValidAllocation) return;

    const teamPieces = state.pieces.filter((p) => p.team === currentTeam).sort((a, b) => a.number - b.number);
    const placements: PiecePlacementDto[] = currentPreset.positions.map((pos) => {
      const piece = teamPieces.find((p) => p.number === pos.number) || teamPieces[pos.number - 1];
      const actualCol = isLeftHalf ? pos.relativeCol : 13 - pos.relativeCol;
      const ability = abilities[pos.number] || pos.defaultAbility;

      return {
        pieceId: piece.id,
        row: pos.row,
        col: actualCol,
        ability,
      };
    });

    await onConfirmSetup(currentTeam, placements);
  };

  return (
    <div className="modal-overlay">
      <div
        className="glass-panel setup-box-advanced"
        style={{
          maxWidth: '920px',
          width: '95%',
          maxHeight: '90vh',
          overflowY: 'auto',
          padding: '24px',
          textAlign: 'left',
        }}
      >
        {/* モーダルヘッダー */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '16px' }}>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Compass size={24} color="#00f0ff" />
              <h2 style={{ fontSize: '24px', fontWeight: 800 }}>タクティクス＆フォーメーション設定</h2>
            </div>
            <div
              style={{
                fontSize: '15px',
                fontWeight: 700,
                marginTop: '4px',
                color: currentTeam === 'TeamA' ? 'var(--teamA-primary)' : 'var(--teamB-primary)',
              }}
            >
              {isHalf1 ? '前半' : '後半'} : {teamName} の配置
            </div>
          </div>

          {/* 能力配分インジケーター */}
          <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
            <div
              className={`allocation-pill ${count3 === 1 ? 'ok' : 'ng'}`}
              title="能力3(エース)は1名必要です"
            >
              ★3 エース: {count3}/1名
            </div>
            <div
              className={`allocation-pill ${count2 === 3 ? 'ok' : 'ng'}`}
              title="能力2(主力)は3名必要です"
            >
              ★2 主力: {count2}/3名
            </div>
            <div
              className={`allocation-pill ${count1 === 7 ? 'ok' : 'ng'}`}
              title="能力1(一般)は7名必要です"
            >
              ★1 一般: {count1}/7名
            </div>
          </div>
        </div>

        {/* 1. フォーメーション15種選択セクション */}
        <div style={{ marginBottom: '20px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
            <span style={{ fontSize: '14px', fontWeight: 700, color: '#00d2ff' }}>
              📋 1. 有名フォーメーションを選択 (全15種類)
            </span>
            <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
              現在: <strong>{currentPreset.name}</strong> ({currentPreset.category})
            </span>
          </div>

          <div className="formation-grid-selector">
            {FORMATION_PRESETS.map((f) => (
              <button
                key={f.id}
                type="button"
                className={`formation-btn-card ${selectedFormationId === f.id ? 'active' : ''}`}
                onClick={() => handleSelectFormation(f.id)}
              >
                <div className="formation-btn-title">{f.name.split(' ')[0]}</div>
                <div className="formation-btn-category">{f.category}</div>
              </button>
            ))}
          </div>

          <p style={{ fontSize: '12px', color: '#a0c0d0', margin: '8px 0 0', lineHeight: 1.5 }}>
            💡 <strong>{currentPreset.name}</strong>: {currentPreset.description}
          </p>
        </div>

        {/* 2. ポジションごとの能力値割り当てセクション */}
        <div style={{ marginBottom: '20px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
            <span style={{ fontSize: '14px', fontWeight: 700, color: '#ffd700' }}>
              ⭐ 2. どのポジションに能力を割り当てるか選択 (★をクリックで切替)
            </span>

            {/* クイックプリセット */}
            <div style={{ display: 'flex', gap: '6px' }}>
              <button
                type="button"
                className="btn-quick-preset"
                onClick={() => applyQuickPreset('fw')}
                title="CFに★3、ウイング・中盤に★2"
              >
                🎯 FWエース型
              </button>
              <button
                type="button"
                className="btn-quick-preset"
                onClick={() => applyQuickPreset('mf')}
                title="トップ下に★3、FW・ボランチに★2"
              >
                🧠 司令塔MF型
              </button>
              <button
                type="button"
                className="btn-quick-preset"
                onClick={() => applyQuickPreset('gk')}
                title="GKに★3、FW・CBに★2"
              >
                🧤 守護神GK型
              </button>
            </div>
          </div>

          {/* 11人のコマ配置＆能力設定カード一覧 */}
          <div className="setup-players-grid">
            {currentPreset.positions.map((pos) => {
              const ability = abilities[pos.number] || 1;
              const isGk = pos.number === 1;

              return (
                <div
                  key={pos.number}
                  className={`player-setup-card ${ability === 3 ? 'card-star-3' : ability === 2 ? 'card-star-2' : ''}`}
                  onClick={() => handleCycleAbility(pos.number)}
                  title="クリックで★1〜3を切り替え"
                >
                  <div className="player-setup-top">
                    <span className="player-setup-pos">{pos.positionName}</span>
                    <span className="player-setup-num">#{pos.number}</span>
                  </div>

                  <div className="player-setup-token-preview">
                    <div className={`preview-piece-token ability-${ability} ${isGk ? 'is-gk' : ''}`}>
                      <span>{pos.number}</span>
                      {isGk && <span className="preview-gk-badge">🧤</span>}
                    </div>
                  </div>

                  {/* 星選択コントロール */}
                  <div className="star-rating-selector" onClick={(e) => e.stopPropagation()}>
                    {[1, 2, 3].map((star) => (
                      <button
                        key={star}
                        type="button"
                        className={`star-choice-btn ${ability === star ? 'selected' : ''}`}
                        onClick={() => handleSetAbility(pos.number, star)}
                      >
                        ★{star}
                      </button>
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* フッターアクションバー */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '16px' }}>
          <div>
            {!isValidAllocation ? (
              <span style={{ fontSize: '13px', color: '#ff4466', fontWeight: 700 }}>
                ⚠️ 能力値の配分が不正です (★3が1名、★2が3名、★1が7名になるように調整してください)
              </span>
            ) : (
              <span style={{ fontSize: '13px', color: '#00ffaa', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '6px' }}>
                <Sparkles size={16} /> 理想的なフォーメーション＆能力配分が完了しました！
              </span>
            )}
          </div>

          <button
            className="btn-primary-action"
            style={{
              padding: '12px 28px',
              fontSize: '15px',
              opacity: isValidAllocation ? 1 : 0.5,
              cursor: isValidAllocation ? 'pointer' : 'not-allowed',
            }}
            disabled={!isValidAllocation}
            onClick={handleConfirm}
          >
            <Shield size={18} />
            <span>✅ この配置と能力で試合開始</span>
          </button>
        </div>
      </div>
    </div>
  );
};
