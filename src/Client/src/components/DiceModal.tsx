import React, { useState, useEffect, useRef } from 'react';
import type { DuelContext, GameState } from '../types/game';
import confetti from 'canvas-confetti';

interface DiceModalProps {
  duel: DuelContext;
  half: number;
  onRollAndResolve: () => Promise<GameState | null>;
  onFinish: (updatedState: GameState) => void;
}

/**
 * 【学習用解説: サイコロ対決モーダル (DiceModal)】
 * 
 * タックル、インターセプト、シュート阻止の「1対1の真剣勝負（デュエル）」をドラマチックに演出するコンポーネントです。
 * 
 * ■ 要件と機能:
 * 1. ピッチ自陣配置に合わせた左右表示:
 *    - 前半 (Half 1): 左が TEAM BLUE (TeamA)、右が TEAM RED (TeamB)
 *    - 後半 (Half 2): 陣地交代により左が TEAM RED (TeamB)、右が TEAM BLUE (TeamA)
 *    - 仕掛けた側・受け側にかかわらず盤面の左右と完全に一致します。
 * 2. 役割バッジ (⚔️ ATTACK / 🛡️ DEFENSE):
 *    - どちらが仕掛けた攻撃側で、どちらが守備側なのかが一目で分かります。
 * 3. 能力値 ＋ サイコロ出目の正確な合計値表示:
 *    - サーバー計算およびクライアント出目を正確に合計値およびメーターに反映します。
 * 4. 判定結果の3秒保持 & スキップ:
 *    - 勝敗確定後3秒間カウントダウン表示し、「OK (盤面に戻る)」で即座に進めることも可能です。
 */
export const DiceModal: React.FC<DiceModalProps> = ({ duel, half, onRollAndResolve, onFinish }) => {
  const [isRolling, setIsRolling] = useState(false);
  const [displayAttackerDice, setDisplayAttackerDice] = useState<number>(1);
  const [displayDefenderDice, setDisplayDefenderDice] = useState<number>(1);
  const [resolvedState, setResolvedState] = useState<GameState | null>(null);
  const [resolvedDuel, setResolvedDuel] = useState<DuelContext | null>(null);
  const [resultMessage, setResultMessage] = useState<string>('');
  const [countdown, setCountdown] = useState<number>(3);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

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
    if (isRolling || resolvedState) return;
    setIsRolling(true);

    // ダイスが高速で回転・変化するアニメーション (1.2秒間)
    const rollInterval = setInterval(() => {
      setDisplayAttackerDice(Math.floor(Math.random() * 6) + 1);
      setDisplayDefenderDice(Math.floor(Math.random() * 6) + 1);
    }, 80);

    setTimeout(async () => {
      clearInterval(rollInterval);

      const nextState = await onRollAndResolve();
      setIsRolling(false);

      if (nextState) {
        setResolvedState(nextState);

        // 最新のログから勝敗結果を取得
        const lastLog = nextState.matchLogs && nextState.matchLogs.length > 0
          ? nextState.matchLogs[nextState.matchLogs.length - 1]
          : '勝負判定が完了しました！';
        setResultMessage(lastLog);

        // 解決後の出目を反映 (サーバーの lastResolvedDuel から取得)
        const currentDuel = nextState.lastResolvedDuel || nextState.pendingDuel || duel;
        setResolvedDuel(currentDuel);

        if (currentDuel.attackerDice != null) {
          setDisplayAttackerDice(currentDuel.attackerDice);
        }
        if (currentDuel.defenderDice != null) {
          setDisplayDefenderDice(currentDuel.defenderDice);
        }

        // 紙吹雪
        confetti({
          particleCount: 60,
          spread: 70,
          origin: { y: 0.6 },
        });

        // 3秒間の保持カウントダウン開始
        setCountdown(3);
      }
    }, 1200);
  };

  // カウントダウン処理 (3秒保持)
  useEffect(() => {
    if (!resolvedState) return;

    if (countdown > 0) {
      timerRef.current = setTimeout(() => {
        setCountdown((prev) => prev - 1);
      }, 1000);
    } else {
      // 3秒経過で閉じる
      onFinish(resolvedState);
    }

    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [resolvedState, countdown, onFinish]);

  const activeDuel = resolvedDuel || duel;

  // 出目と合計値の計算 (確定後は出目を確実に加算)
  const attackerDiceVal = activeDuel.attackerDice ?? displayAttackerDice;
  const defenderDiceVal = activeDuel.defenderDice ?? displayDefenderDice;

  const attackerTotal = activeDuel.attackerAbilitySum + attackerDiceVal;
  const defenderTotal = activeDuel.defenderAbilitySum + defenderDiceVal;

  // ==========================================
  // ピッチ自陣配置に合わせた左右チーム判定
  // 前半 (half == 1): 左 = TeamA (BLUE), 右 = TeamB (RED)
  // 後半 (half == 2): 左 = TeamB (RED), 右 = TeamA (BLUE)
  // ==========================================
  const leftTeam = half === 1 ? 'TeamA' : 'TeamB';
  const rightTeam = half === 1 ? 'TeamB' : 'TeamA';

  const isLeftAttacker = activeDuel.attackingTeam === leftTeam;
  const isRightAttacker = activeDuel.attackingTeam === rightTeam;

  const leftDiceVal = isLeftAttacker ? attackerDiceVal : defenderDiceVal;
  const rightDiceVal = isRightAttacker ? attackerDiceVal : defenderDiceVal;

  const leftTotal = isLeftAttacker ? attackerTotal : defenderTotal;
  const rightTotal = isRightAttacker ? attackerTotal : defenderTotal;

  const leftAbilitySum = isLeftAttacker ? activeDuel.attackerAbilitySum : activeDuel.defenderAbilitySum;
  const rightAbilitySum = isRightAttacker ? activeDuel.attackerAbilitySum : activeDuel.defenderAbilitySum;

  const leftParticipants = isLeftAttacker ? activeDuel.attackers : activeDuel.defenders;
  const rightParticipants = isRightAttacker ? activeDuel.attackers : activeDuel.defenders;

  const leftColor = leftTeam === 'TeamA' ? 'var(--teamA-primary)' : 'var(--teamB-primary)';
  const rightColor = rightTeam === 'TeamA' ? 'var(--teamA-primary)' : 'var(--teamB-primary)';

  const leftName = leftTeam === 'TeamA' ? 'TEAM BLUE' : 'TEAM RED';
  const rightName = rightTeam === 'TeamA' ? 'TEAM BLUE' : 'TEAM RED';

  // GK手守備ボーナスやシュートコース上DF補正の有無
  const leftGkParticipant = !isLeftAttacker ? activeDuel.defenders.find((d) => d.isGoalkeeper && d.abilityBonus > 0) : null;
  const rightGkParticipant = !isRightAttacker ? activeDuel.defenders.find((d) => d.isGoalkeeper && d.abilityBonus > 0) : null;

  // 下部メーターの計算 (左チーム vs 右チーム)
  const totalSum = Math.max(1, leftTotal + rightTotal);
  const leftPercent = Math.round((leftTotal / totalSum) * 100);
  const rightPercent = 100 - leftPercent;

  return (
    <div className="modal-overlay">
      <div className="dice-modal-content">
        <div className={`duel-title-badge ${activeDuel.type}`}>
          {activeDuel.type === 'Tackle'
            ? '⚔️ TACKLE DUEL'
            : activeDuel.type === 'Intercept'
            ? '🛡️ INTERCEPT DUEL'
            : '🔥 GOAL SHOOTOUT DUEL'}
        </div>

        <h3 style={{ fontSize: '20px', fontWeight: 800, margin: '6px 0' }}>
          {resolvedState ? (resultMessage || activeDuel.message) : activeDuel.message}
        </h3>

        <div className="duel-arena">
          {/* 左側チーム (ピッチ自陣の左側) */}
          <div className="duel-fighter">
            {/* 役割バッジ (攻撃 or 守備) */}
            <div
              style={{
                fontSize: '12px',
                fontWeight: 800,
                padding: '3px 10px',
                borderRadius: '12px',
                display: 'inline-block',
                marginBottom: '4px',
                background: isLeftAttacker ? 'rgba(0, 210, 255, 0.2)' : 'rgba(255, 51, 102, 0.2)',
                border: `1px solid ${isLeftAttacker ? 'var(--teamA-primary)' : 'var(--teamB-primary)'}`,
                color: isLeftAttacker ? 'var(--teamA-primary)' : 'var(--teamB-primary)',
              }}
            >
              {isLeftAttacker ? '⚔️ ATTACK (仕掛け側)' : '🛡️ DEFENSE (受け側)'}
            </div>

            <span className="fighter-name" style={{ color: leftColor }}>
              {leftName}
              <div style={{ fontSize: '15px', color: 'var(--text-muted)', marginTop: '4px' }}>
                {leftParticipants.map((p) =>
                  p.abilityBonus > 0
                    ? `${p.name} (★${p.ability} + 🧤補正+${p.abilityBonus})`
                    : `${p.name} (★${p.ability})`
                ).join(', ')}
              </div>
            </span>

            {leftGkParticipant && (
              <div className="gk-save-bonus-badge" style={{ fontSize: '13px', padding: '4px 10px', margin: '4px 0' }}>
                🧤 GK手守備＆コース補正: 能力+{leftGkParticipant.abilityBonus}
              </div>
            )}

            <div className="fighter-ability-badge">
              {isLeftAttacker ? '基礎能力合計: ' : '能力合計: '}
              <span className="fighter-ability-val">{leftAbilitySum}</span>
            </div>

            {/* 3Dサイコロ */}
            <div className={`dice-cube ${isRolling ? 'rolling' : ''}`}>
              {getDiceIcon(leftDiceVal)}
            </div>

            <div style={{ fontSize: '22px', fontWeight: 800 }}>
              出目: +{leftDiceVal}
            </div>
          </div>

          <div className="vs-divider">VS</div>

          {/* 右側チーム (ピッチ自陣の右側) */}
          <div className="duel-fighter">
            {/* 役割バッジ (攻撃 or 守備) */}
            <div
              style={{
                fontSize: '12px',
                fontWeight: 800,
                padding: '3px 10px',
                borderRadius: '12px',
                display: 'inline-block',
                marginBottom: '4px',
                background: isRightAttacker ? 'rgba(0, 210, 255, 0.2)' : 'rgba(255, 51, 102, 0.2)',
                border: `1px solid ${isRightAttacker ? 'var(--teamA-primary)' : 'var(--teamB-primary)'}`,
                color: isRightAttacker ? 'var(--teamA-primary)' : 'var(--teamB-primary)',
              }}
            >
              {isRightAttacker ? '⚔️ ATTACK (仕掛け側)' : '🛡️ DEFENSE (受け側)'}
            </div>

            <span className="fighter-name" style={{ color: rightColor }}>
              {rightName}
              <div style={{ fontSize: '15px', color: 'var(--text-muted)', marginTop: '4px' }}>
                {rightParticipants.map((p) =>
                  p.abilityBonus > 0
                    ? `${p.name} (★${p.ability} + 🧤補正+${p.abilityBonus})`
                    : `${p.name} (★${p.ability})`
                ).join(', ')}
              </div>
            </span>

            {rightGkParticipant && (
              <div className="gk-save-bonus-badge" style={{ fontSize: '13px', padding: '4px 10px', margin: '4px 0' }}>
                🧤 GK手守備＆コース補正: 能力+{rightGkParticipant.abilityBonus}
              </div>
            )}

            <div className="fighter-ability-badge">
              {isRightAttacker ? '基礎能力合計: ' : '能力合計: '}
              <span className="fighter-ability-val">{rightAbilitySum}</span>
            </div>

            {/* 3Dサイコロ */}
            <div className={`dice-cube ${isRolling ? 'rolling' : ''}`}>
              {getDiceIcon(rightDiceVal)}
            </div>

            <div style={{ fontSize: '22px', fontWeight: 800 }}>
              出目: +{rightDiceVal}
            </div>
          </div>
        </div>

        {/* 比較メーター (左チーム vs 右チーム) */}
        <div className="duel-meter-container">
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '20px', fontWeight: 800 }}>
            <span style={{ color: leftColor }}>合計: {leftTotal}</span>
            <span style={{ color: rightColor }}>合計: {rightTotal}</span>
          </div>

          <div className="meter-track">
            <div
              style={{
                width: `${leftPercent}%`,
                background: leftColor,
                height: '100%',
                transition: 'width 0.3s ease',
              }}
            />
            <div
              style={{
                width: `${rightPercent}%`,
                background: rightColor,
                height: '100%',
                transition: 'width 0.3s ease',
              }}
            />
          </div>
        </div>

        {/* サイコロを振るボタン / 判定完了後の3秒保持 & 閉じるボタン */}
        {!activeDuel.isResolved && !resolvedState && (
          <button
            className="btn-roll-dice"
            onClick={handleRoll}
            disabled={isRolling}
          >
            {isRolling ? 'サイコロを振っています...' : '🎲 サイコロを振って勝負！'}
          </button>
        )}

        {resolvedState && (
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px', marginTop: '12px' }}>
            <div style={{ fontSize: '14px', color: 'var(--accent-glow)', fontWeight: 600 }}>
              ⏳ 結果を表示中... ({countdown}秒後に盤面へ戻ります)
            </div>
            <button
              className="btn-action"
              style={{
                padding: '10px 24px',
                background: 'linear-gradient(135deg, #00d2ff, #0072ff)',
                color: '#fff',
                fontWeight: 700,
                borderRadius: '8px',
                border: 'none',
                cursor: 'pointer',
              }}
              onClick={() => onFinish(resolvedState)}
            >
              OK (盤面に戻る)
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
