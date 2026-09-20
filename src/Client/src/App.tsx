import { useEffect, useState } from 'react';
import type { GameState, TeamType } from './types/game';
import { api } from './services/api';
import { Header } from './components/Header';
import { ScoreBoard } from './components/ScoreBoard';
import { Board } from './components/Board';
import { ControlBar } from './components/ControlBar';
import { DiceModal } from './components/DiceModal';
import { SetupModal } from './components/SetupModal';
import { GoalModal } from './components/GoalModal';
import { RuleGuideModal } from './components/RuleGuideModal';
import { MatchLogs } from './components/MatchLogs';
import './styles/index.css';
import './styles/board.css';
import './styles/components.css';

export function App() {
  const [state, setState] = useState<GameState | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [selectedPieceId, setSelectedPieceId] = useState<string | null>(null);
  const [isPassMode, setIsPassMode] = useState(false);
  const [isRulesOpen, setIsRulesOpen] = useState(false);

  // 初期ロード
  useEffect(() => {
    loadGameState();
  }, []);

  const loadGameState = async () => {
    try {
      setLoading(true);
      const data = await api.getGameState();
      setState(data);
      setError(null);
    } catch (err: any) {
      setError(err.message || 'ゲームの初期化に失敗しました。');
    } finally {
      setLoading(false);
    }
  };

  const handleSelectPiece = (pieceId: string) => {
    if (!state) return;
    if (selectedPieceId === pieceId) {
      setSelectedPieceId(null);
    } else {
      setSelectedPieceId(pieceId);
      setIsPassMode(false);
    }
  };

  const handleMovePiece = async (pieceId: string, row: number, col: number) => {
    try {
      const updated = await api.movePiece(pieceId, row, col);
      setState(updated);
      setSelectedPieceId(null);
      setError(null);
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handlePassOrShot = async (row: number, col: number) => {
    try {
      const updated = await api.passOrShot(row, col);
      setState(updated);
      setIsPassMode(false);
      setSelectedPieceId(null);
      setError(null);
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleResolveDuel = async () => {
    try {
      const updated = await api.resolveDuel();
      setState(updated);
      setError(null);
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleEndTurn = async () => {
    try {
      const updated = await api.endTurn();
      setState(updated);
      setSelectedPieceId(null);
      setIsPassMode(false);
      setError(null);
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleResetGame = async () => {
    if (window.confirm('ゲームをリセットして最初からやり直しますか？')) {
      try {
        const updated = await api.resetGame();
        setState(updated);
        setSelectedPieceId(null);
        setIsPassMode(false);
        setError(null);
      } catch (err: any) {
        setError(err.message);
      }
    }
  };

  const handleApplyDefaultSetup = async (team: TeamType) => {
    try {
      const updated = await api.applyDefaultFormation(team);
      setState(updated);
      setError(null);
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleConfirmSetup = async (team: TeamType) => {
    if (!state) return;
    try {
      const teamPieces = state.pieces.filter((p) => p.team === team);
      const placements = teamPieces.map((p) => ({
        pieceId: p.id,
        row: p.position.row,
        col: p.position.col,
      }));
      const updated = await api.setupTeam(team, placements);
      setState(updated);
      setError(null);
    } catch (err: any) {
      setError(err.message);
    }
  };

  if (loading) {
    return (
      <div style={{ display: 'flex', height: '100vh', alignItems: 'center', justifyContent: 'center', color: '#00d2ff', fontSize: '20px', fontWeight: 700 }}>
        ⚽ Positional Tactics をロード中...
      </div>
    );
  }

  if (!state) {
    return (
      <div style={{ display: 'flex', height: '100vh', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: '16px' }}>
        <p style={{ color: '#ff3366' }}>{error || 'ゲーム状態の読み込みに失敗しました。'}</p>
        <button className="btn-secondary" onClick={loadGameState}>再試行</button>
      </div>
    );
  }

  const isSetupPhase =
    state.phase === 'SetupFirstHalfA' ||
    state.phase === 'SetupFirstHalfB' ||
    state.phase === 'SetupSecondHalfA' ||
    state.phase === 'SetupSecondHalfB';

  return (
    <div className="game-root">
      <Header
        onReset={handleResetGame}
        onOpenRules={() => setIsRulesOpen(true)}
      />

      {error && (
        <div
          style={{
            maxWidth: '900px',
            margin: '0 auto 8px',
            padding: '8px 16px',
            background: 'rgba(255, 51, 102, 0.2)',
            border: '1px solid #ff3366',
            borderRadius: '8px',
            color: '#ff6688',
            fontSize: '13px',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <span>⚠️ {error}</span>
          <button
            style={{ background: 'none', color: '#fff', fontSize: '16px' }}
            onClick={() => setError(null)}
          >
            ✕
          </button>
        </div>
      )}

      <ScoreBoard state={state} />

      <ControlBar
        state={state}
        isPassMode={isPassMode}
        onTogglePassMode={() => {
          setIsPassMode(!isPassMode);
          setSelectedPieceId(null);
        }}
        onEndTurn={handleEndTurn}
      />

      <Board
        state={state}
        selectedPieceId={selectedPieceId}
        isPassMode={isPassMode}
        onSelectPiece={handleSelectPiece}
        onMovePiece={handleMovePiece}
        onPassOrShot={handlePassOrShot}
      />

      <MatchLogs logs={state.matchLogs} />

      {/* サイコロ判定演出モーダル */}
      {state.pendingDuel && (
        <DiceModal
          duel={state.pendingDuel}
          onRollAndResolve={handleResolveDuel}
        />
      )}

      {/* 初期配置モーダル */}
      {isSetupPhase && (
        <SetupModal
          state={state}
          onApplyDefault={handleApplyDefaultSetup}
          onConfirmSetup={handleConfirmSetup}
        />
      )}

      {/* ハーフタイム & ゲームオーバー演出 */}
      <GoalModal
        state={state}
        onNextHalf={() => {
          // ハーフタイムから後半配置へ進む処理
          handleApplyDefaultSetup('TeamA');
        }}
        onRestart={handleResetGame}
      />

      {/* ルール説明モーダル */}
      {isRulesOpen && (
        <RuleGuideModal onClose={() => setIsRulesOpen(false)} />
      )}
    </div>
  );
}

export default App;
