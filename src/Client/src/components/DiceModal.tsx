import React, { useState, useEffect, useRef } from 'react';
import type { DuelContext, GameState } from '../types/game';
import confetti from 'canvas-confetti';

interface DiceModalProps {
  duel: DuelContext;
  onRollAndResolve: () => Promise<GameState | null>;
  onFinish: (updatedState: GameState) => void;
}

/**
 * 【学習用解説: サイコロ対決モーダル (DiceModal)】
 * 
 * タックル、インターセプト、シュート阻止の「1対1の真剣勝負（デュエル）」をドラマチックに演出するコンポーネントです。
 * 
 * ■ アニメーションとUX（ユーザー体験）の技術ポイント:
 * 1. ダイス回転アニメーション:
 *    - `setInterval` で 80ms ごとにランダムな出目を激しく切り替え、サイコロが転がっている臨場感を表現。
 * 2. 判定結果の「3秒間保持」:
 *    - サーバーで計算された最新の出目や勝敗メッセージを受け取った後、
 *      すぐにモーダルを閉じずに約3秒間カウントダウン表示を維持し、結果をじっくり味わえるようにしています。
 * 3. 即時スキップ機能:
 *    - 「OK (盤面に戻る)」ボタンを用意し、プレイヤーが自分のペースでゲームを進められるよう配慮しています。
 * 4. 紙吹雪演出:
 *    - `canvas-confetti` ライブラリを使用して、勝利時・ゴール時に華やかな紙吹雪を舞わせます。
 */
export const DiceModal: React.FC<DiceModalProps> = ({ duel, onRollAndResolve, onFinish }) => {
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

        // 解決後の出目を反映 (もしサーバーのログやduelから取得できれば更新)
        // デュエル解決直後の情報
        const currentDuel = nextState.pendingDuel || duel;
        setResolvedDuel(currentDuel);

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
  const attackerDiceVal = activeDuel.attackerDice ?? displayAttackerDice;
  const defenderDiceVal = activeDuel.defenderDice ?? displayDefenderDice;

  const attackerTotal = activeDuel.attackerAbilitySum + (isRolling ? displayAttackerDice : (activeDuel.attackerDice ?? 0));
  const defenderTotal = activeDuel.defenderAbilitySum + (isRolling ? displayDefenderDice : (activeDuel.defenderDice ?? 0));

  const totalSum = Math.max(1, attackerTotal + defenderTotal);
  const attackerPercent = Math.round((attackerTotal / totalSum) * 100);
  const defenderPercent = 100 - attackerPercent;

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
          {/* アタッカー (仕掛けた側) */}
          <div className="duel-fighter">
            <span
              className="fighter-name"
              style={{
                color: activeDuel.attackingTeam === 'TeamA' ? 'var(--teamA-primary)' : 'var(--teamB-primary)',
              }}
            >
              {activeDuel.attackingTeam === 'TeamA' ? 'TEAM BLUE' : 'TEAM RED'}
              <div style={{ fontSize: '15px', color: 'var(--text-muted)', marginTop: '4px' }}>
                {activeDuel.attackers.map((a) => `${a.name} (★${a.ability})`).join(', ')}
              </div>
            </span>

            <div className="fighter-ability-badge">
              基礎能力合計: <span className="fighter-ability-val">{activeDuel.attackerAbilitySum}</span>
            </div>

            {/* 3Dサイコロ */}
            <div className={`dice-cube ${isRolling ? 'rolling' : ''}`}>
              {getDiceIcon(attackerDiceVal)}
            </div>

            <div style={{ fontSize: '22px', fontWeight: 800 }}>
              出目: +{attackerDiceVal}
            </div>
          </div>

          <div className="vs-divider">VS</div>

          {/* ディフェンダー (受け側) */}
          <div className="duel-fighter">
            <span
              className="fighter-name"
              style={{
                color: activeDuel.defendingTeam === 'TeamA' ? 'var(--teamA-primary)' : 'var(--teamB-primary)',
              }}
            >
              {activeDuel.defendingTeam === 'TeamA' ? 'TEAM BLUE' : 'TEAM RED'}
              <div style={{ fontSize: '15px', color: 'var(--text-muted)', marginTop: '4px' }}>
                {activeDuel.defenders.map((d) =>
                  d.abilityBonus > 0
                    ? `${d.name} (★${d.ability} + 🧤手守備+${d.abilityBonus})`
                    : `${d.name} (★${d.ability})`
                ).join(', ')}
              </div>
            </span>

            {activeDuel.hasGkHandBonus && (
              <div className="gk-save-bonus-badge" style={{ fontSize: '14px', padding: '4px 12px' }}>
                🧤 GK手を使った守備: 能力+1
              </div>
            )}

            <div className="fighter-ability-badge">
              能力合計: <span className="fighter-ability-val">{activeDuel.defenderAbilitySum}</span>
            </div>

            {/* 3Dサイコロ */}
            <div className={`dice-cube ${isRolling ? 'rolling' : ''}`}>
              {getDiceIcon(defenderDiceVal)}
            </div>

            <div style={{ fontSize: '22px', fontWeight: 800 }}>
              出目: +{defenderDiceVal}
            </div>
          </div>
        </div>

        {/* 比較メーター */}
        <div className="duel-meter-container">
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '20px', fontWeight: 800 }}>
            <span style={{ color: 'var(--teamA-primary)' }}>合計: {attackerTotal}</span>
            <span style={{ color: 'var(--teamB-primary)' }}>合計: {defenderTotal}</span>
          </div>

          <div className="meter-track">
            <div className="meter-fill-attacker" style={{ width: `${attackerPercent}%` }} />
            <div className="meter-fill-defender" style={{ width: `${defenderPercent}%` }} />
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
