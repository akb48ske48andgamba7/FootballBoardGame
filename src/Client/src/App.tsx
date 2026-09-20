import { useEffect, useState, useRef } from 'react';
import type { GameMode, GameState, PiecePlacementDto, TeamType } from './types/game';
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
  const [isCpuThinking, setIsCpuThinking] = useState(false);

  const [selectedPieceId, setSelectedPieceId] = useState<string | null>(null);
  const [isPassMode, setIsPassMode] = useState(false);
  const [isRulesOpen, setIsRulesOpen] = useState(false);

  const isCpuRunningRef = useRef(false);

  // 初期ロード
  useEffect(() => {
    loadGameState();
  }, []);

  // CPU手番の自動実行エフェクト
  useEffect(() => {
    if (!state || state.mode !== 'PvC' || isCpuRunningRef.current) return;

    // CPUの初期配置フェーズ
    const isCpuSetup =
      state.phase === 'SetupFirstHalfB' || state.phase === 'SetupSecondHalfB';

    // CPUのターン（試合中かつ未解決デュエルなし）
    const isCpuTurn =
      (state.phase === 'FirstHalf' || state.phase === 'SecondHalf') &&
      state.activeTeam === 'TeamB' &&
      state.pendingDuel === null;

    if (isCpuSetup || isCpuTurn) {
      isCpuRunningRef.current = true;
      setIsCpuThinking(true);

      const delayMs = isCpuSetup ? 600 : 900;
      const timer = setTimeout(async () => {
        try {
          const updated = await api.executeCpuStep();
          setState(updated);
          setError(null);
        } catch (err: any) {
          setError(err.message);
          // 万一の失敗時も最新状態を再同期
          loadGameState();
        } finally {
          setIsCpuThinking(false);
          isCpuRunningRef.current = false;
        }
      }, delayMs);

      return () => {
        clearTimeout(timer);
        isCpuRunningRef.current = false;
      };
    }
  }, [state]);

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

  const handleSelectMode = async (mode: GameMode) => {
    try {
      const updated = await api.setGameMode(mode);
      setState(updated);
      setError(null);
    } catch (err: any) {
      setError(err.message);
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

  const handleResolveDuel = async (): Promise<GameState | null> => {
    try {
      const updated = await api.resolveDuel();
      setError(null);
      return updated;
    } catch (err: any) {
      setError(err.message);
      return null;
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

  const handleConfirmSetup = async (team: TeamType, customPlacements?: PiecePlacementDto[]) => {
    if (!state) return;
    try {
      let placements: PiecePlacementDto[];
      if (customPlacements && customPlacements.length === 11) {
        placements = customPlacements;
      } else {
        const teamPieces = state.pieces.filter((p) => p.team === team);
        placements = teamPieces.map((p) => ({
          pieceId: p.id,
          row: p.position.row,
          col: p.position.col,
          ability: p.ability,
        }));
      }
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

  // 人間のプレイヤーが操作すべき初期配置フェーズか
  const isHumanSetupPhase =
    state.phase === 'SetupFirstHalfA' ||
    state.phase === 'SetupSecondHalfA' ||
    (state.mode === 'PvP' && (state.phase === 'SetupFirstHalfB' || state.phase === 'SetupSecondHalfB'));

  const isUserTurn = state.mode === 'PvP' || state.activeTeam === 'TeamA';

  return (
    <div className="game-root">
      <Header
        mode={state.mode}
        onSelectMode={handleSelectMode}
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
          <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
            {state.activeTeam === 'TeamB' && (
              <button
                className="btn-action"
                style={{
                  fontSize: '12px',
                  padding: '4px 10px',
                  background: 'linear-gradient(135deg, #ff3366, #ff6688)',
                  color: '#fff',
                  borderRadius: '6px',
                  border: 'none',
                  cursor: 'pointer',
                  fontWeight: 700,
                }}
                onClick={async () => {
                  try {
                    const updated = await api.endTurn();
                    setState(updated);
                    setError(null);
                  } catch (e: any) {
                    loadGameState();
                  }
                }}
              >
                手番を交代して再開
              </button>
            )}
            <button
              style={{ background: 'none', color: '#fff', fontSize: '16px', border: 'none', cursor: 'pointer' }}
              onClick={() => setError(null)}
            >
              ✕
            </button>
          </div>
        </div>
      )}

      <ScoreBoard state={state} isCpuThinking={isCpuThinking} />

      <ControlBar
        state={state}
        isPassMode={isPassMode}
        onTogglePassMode={() => {
          if (!isUserTurn) return;
          setIsPassMode(!isPassMode);
          setSelectedPieceId(null);
        }}
        onEndTurn={() => {
          if (!isUserTurn) return;
          handleEndTurn();
        }}
      />

      <Board
        state={state}
        selectedPieceId={selectedPieceId}
        isPassMode={isPassMode}
        onSelectPiece={(id) => {
          if (!isUserTurn) return;
          handleSelectPiece(id);
        }}
        onMovePiece={(id, r, c) => {
          if (!isUserTurn) return;
          handleMovePiece(id, r, c);
        }}
        onPassOrShot={(r, c) => {
          if (!isUserTurn) return;
          handlePassOrShot(r, c);
        }}
      />

      <MatchLogs logs={state.matchLogs} />

      {/* サイコロ判定演出モーダル */}
      {state.pendingDuel && (
        <DiceModal
          duel={state.pendingDuel}
          onRollAndResolve={handleResolveDuel}
          onFinish={(updatedState) => {
            setState(updatedState);
            setError(null);
          }}
        />
      )}

      {/* 初期配置モーダル (人間プレイヤーの手番時のみ表示) */}
      {isHumanSetupPhase && (
        <SetupModal
          state={state}
          onConfirmSetup={handleConfirmSetup}
        />
      )}

      {/* ハーフタイム & ゲームオーバー演出 */}
      <GoalModal
        state={state}
        onNextHalf={() => {
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
