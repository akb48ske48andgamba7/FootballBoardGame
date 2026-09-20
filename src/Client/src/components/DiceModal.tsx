import React, { useState } from 'react';
import type { DuelContext } from '../types/game';
import confetti from 'canvas-confetti';

interface DiceModalProps {
  duel: DuelContext;
  onRollAndResolve: () => Promise<void>;
}

export const DiceModal: React.FC<DiceModalProps> = ({ duel, onRollAndResolve }) => {
  const [isRolling, setIsRolling] = useState(false);
  const [displayAttackerDice, setDisplayAttackerDice] = useState<number>(1);
  const [displayDefenderDice, setDisplayDefenderDice] = useState<number>(1);
  const [hasResolvedLocally, setHasResolvedLocally] = useState(false);

  const getDiceIcon = (num: number) => {
    switch (num) {
      case 1: return '⚀';
      case 2: return '⚁';
      case 3: return '⚂';
      case 4: return '⚃';
      case 5: return '⚄';
      case 6: return '⚅';
      default: return '🎲';
    }
  };

  const handleRoll = async () => {
    if (isRolling || hasResolvedLocally) return;
    setIsRolling(true);

    // ダイスが高速で回転・変化するアニメーション (1.2秒間)
    const rollInterval = setInterval(() => {
      setDisplayAttackerDice(Math.floor(Math.random() * 6) + 1);
      setDisplayDefenderDice(Math.floor(Math.random() * 6) + 1);
    }, 80);

    setTimeout(async () => {
      clearInterval(rollInterval);
      await onRollAndResolve();
      setIsRolling(false);
      setHasResolvedLocally(true);

      // ゴールや勝利時に紙吹雪
      confetti({
        particleCount: 50,
        spread: 60,
        origin: { y: 0.6 },
      });
    }, 1200);
  };

  const attackerDiceVal = duel.attackerDice ?? displayAttackerDice;
  const defenderDiceVal = duel.defenderDice ?? displayDefenderDice;

  const attackerTotal = duel.attackerAbilitySum + (isRolling ? displayAttackerDice : (duel.attackerDice ?? 0));
  const defenderTotal = duel.defenderAbilitySum + (isRolling ? displayDefenderDice : (duel.defenderDice ?? 0));

  const totalSum = Math.max(1, attackerTotal + defenderTotal);
  const attackerPercent = Math.round((attackerTotal / totalSum) * 100);
  const defenderPercent = 100 - attackerPercent;

  return (
    <div className="modal-overlay">
      <div className="dice-modal-content">
        <div className={`duel-title-badge ${duel.type}`}>
          {duel.type === 'Tackle'
            ? '⚔️ TACKLE DUEL'
            : duel.type === 'Intercept'
            ? '🛡️ INTERCEPT DUEL'
            : '🔥 GOAL SHOOTOUT DUEL'}
        </div>

        <h3 style={{ fontSize: '20px', fontWeight: 800, margin: '6px 0' }}>
          {duel.message}
        </h3>

        <div className="duel-arena">
          {/* アタッカー (仕掛けた側) */}
          <div className="duel-fighter">
            <span
              className="fighter-name"
              style={{
                color: duel.attackingTeam === 'TeamA' ? 'var(--teamA-primary)' : 'var(--teamB-primary)',
              }}
            >
              {duel.attackingTeam === 'TeamA' ? 'TEAM BLUE' : 'TEAM RED'}
              <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                {duel.attackers.map((a) => `${a.name} (★${a.ability})`).join(', ')}
              </div>
            </span>

            <div className="fighter-ability-badge">
              基礎能力合計: <span className="fighter-ability-val">{duel.attackerAbilitySum}</span>
            </div>

            {/* 3Dサイコロ */}
            <div className={`dice-cube ${isRolling ? 'rolling' : ''}`}>
              {getDiceIcon(attackerDiceVal)}
            </div>

            <div style={{ fontSize: '13px', fontWeight: 700 }}>
              出目: +{attackerDiceVal}
            </div>
          </div>

          <div className="vs-divider">VS</div>

          {/* ディフェンダー (受け側) */}
          <div className="duel-fighter">
            <span
              className="fighter-name"
              style={{
                color: duel.defendingTeam === 'TeamA' ? 'var(--teamA-primary)' : 'var(--teamB-primary)',
              }}
            >
              {duel.defendingTeam === 'TeamA' ? 'TEAM BLUE' : 'TEAM RED'}
              <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                {duel.defenders.map((d) => `${d.name} (★${d.ability})`).join(', ')}
              </div>
            </span>

            <div className="fighter-ability-badge">
              基礎能力合計: <span className="fighter-ability-val">{duel.defenderAbilitySum}</span>
            </div>

            {/* 3Dサイコロ */}
            <div className={`dice-cube ${isRolling ? 'rolling' : ''}`}>
              {getDiceIcon(defenderDiceVal)}
            </div>

            <div style={{ fontSize: '13px', fontWeight: 700 }}>
              出目: +{defenderDiceVal}
            </div>
          </div>
        </div>

        {/* 比較メーター */}
        <div className="duel-meter-container">
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '13px', fontWeight: 700 }}>
            <span style={{ color: 'var(--teamA-primary)' }}>合計: {attackerTotal}</span>
            <span style={{ color: 'var(--teamB-primary)' }}>合計: {defenderTotal}</span>
          </div>

          <div className="meter-track">
            <div className="meter-fill-attacker" style={{ width: `${attackerPercent}%` }} />
            <div className="meter-fill-defender" style={{ width: `${defenderPercent}%` }} />
          </div>
        </div>

        {/* サイコロを振るボタン */}
        {!duel.isResolved && !hasResolvedLocally && (
          <button
            className="btn-roll-dice"
            onClick={handleRoll}
            disabled={isRolling}
          >
            {isRolling ? 'サイコロを振っています...' : '🎲 サイコロを振って勝負！'}
          </button>
        )}
      </div>
    </div>
  );
};
