import React from 'react';
import type { GameState, Piece, Position } from '../types/game';
import { PieceToken } from './PieceToken';

interface BoardProps {
  state: GameState;
  selectedPieceId: string | null;
  isPassMode: boolean;
  onSelectPiece: (pieceId: string) => void;
  onMovePiece: (pieceId: string, row: number, col: number) => void;
  onPassOrShot: (row: number, col: number) => void;
}

/**
 * 【学習用解説: サッカー盤面を描画する Board コンポーネント】
 * 
 * 現代サッカーの理論「ポジショナルプレー (5レーン理論をさらに発展させた7レーン)」を
 * 盤面上に可視化し、プレイヤーの直感的な操作（コマ移動・パス・シュート）を実現するビュー層です。
 * 
 * ■ 設計とUI技術のポイント:
 * 1. CSS Grid による 7分割 × 12マス のピッチ構成:
 *    - 縦方向: 7レーン（上サイド、上ハーフ、上インサイド、センター、下インサイド、下ハーフ、下サイド）
 *    - 横方向: 12マス（ゴール間の攻防）
 * 2. 視覚的フィードバック（ハイライト）:
 *    - コマ選択時: 移動可能な周囲1〜2マスの緑色ハイライト (`valid-move`)
 *    - パスモード時: 直線方向のパス可能マス (`valid-pass`)
 *    - オフサイドライン: 相手最後尾DFの列を赤いレーザーライン (`offside-laser-vertical`) で常時描画
 * 3. ペナルティエリアの境界線:
 *    - 左右それぞれ Col 1〜2 / Col 11〜12, Row 3〜5 の計6マスに枠線を美しく描画
 */
export const Board: React.FC<BoardProps> = ({
  state,
  selectedPieceId,
  isPassMode,
  onSelectPiece,
  onMovePiece,
  onPassOrShot,
}) => {
  const lanes = ['上サイド', '上ハーフ', '上インサイド', 'センター', '下インサイド', '下ハーフ', '下サイド'];

  const selectedPiece = state.pieces.find((p) => p.id === selectedPieceId);
  const ballHolder = state.pieces.find((p) => p.id === state.ball.holderPieceId);
  // 各コマの移動可否判定 (通常移動枠が残っていれば同一選手でも移動可能)
  const canPieceMoveClient = (piece: Piece): boolean => {
    if (piece.team !== state.activeTeam) return false;
    const action = state.currentTurnAction;
    if ((action?.standardMovesRemaining ?? 0) > 0) {
      return true;
    }
    return piece.isGoalkeeper && (action?.gkBonusAvailable ?? false) && !(action?.hasUsedGkBonusMove ?? false);
  };

  // 攻撃目標ゴール (横方向)
  // 前半: TeamAは右 (Row 4, Col 13)、TeamBは左 (Row 4, Col 0)
  // 後半: TeamAは左 (Row 4, Col 0)、TeamBは右 (Row 4, Col 13)
  const targetGoalPos: Position =
    (state.half === 1 && state.activeTeam === 'TeamA') ||
    (state.half === 2 && state.activeTeam === 'TeamB')
      ? { row: 4, col: 13 }
      : { row: 4, col: 0 };

  const isPlaying = state.phase === 'FirstHalf' || state.phase === 'SecondHalf';

  // 移動可能マスの計算 (最大2マス)
  const isCellValidMove = (row: number, col: number): boolean => {
    if (!isPlaying || isPassMode || !selectedPiece) return false;
    const dRow = Math.abs(selectedPiece.position.row - row);
    const dCol = Math.abs(selectedPiece.position.col - col);
    const dist = Math.max(dRow, dCol);
    if (dist < 1 || dist > 2) return false;

    // 移動先に相手ボール保持者がいる場合（タックル）のチェック
    const opposingTeam = state.activeTeam === 'TeamA' ? 'TeamB' : 'TeamA';
    const enemyBallHolder = state.pieces.find(
      (p) => p.team === opposingTeam && p.position.row === row && p.position.col === col && state.ball.holderPieceId === p.id
    );

    if (enemyBallHolder) {
      // 要件: 相手GKがボールを保持しているときはタックル不可
      if (enemyBallHolder.isGoalkeeper) return false;

      // 要件: 同一選手によるタックルは1ターンに1回まで
      if (state.currentTurnAction?.tackledPieceIds?.includes(selectedPiece.id)) return false;
    }

    return true;
  };

  // パス/シュート可能マスの計算 (縦・横・斜めの直線、かつ6マス以内)
  const isCellValidPass = (row: number, col: number): boolean => {
    if (!isPlaying || !isPassMode || !ballHolder) return false;
    const startRow = ballHolder.position.row;
    const startCol = ballHolder.position.col;
    if (startRow === row && startCol === col) return false;

    const dRow = Math.abs(startRow - row);
    const dCol = Math.abs(startCol - col);
    if (dRow !== 0 && dCol !== 0 && dRow !== dCol) return false;

    // 要件: 前後左右斜め6マス以内制限
    const dist = Math.max(dRow, dCol);
    return dist <= 6;
  };

  // シュート可能な直線上のゴールかどうかの判定 (縦・横・斜めの直線、かつ6マス以内)
  const isGoalTargetable = (goalPos: Position): boolean => {
    if (!isPlaying || !isPassMode || !ballHolder) return false;
    if (goalPos.row !== targetGoalPos.row || goalPos.col !== targetGoalPos.col) return false;

    const startRow = ballHolder.position.row;
    const startCol = ballHolder.position.col;
    const dRow = Math.abs(startRow - goalPos.row);
    const dCol = Math.abs(startCol - goalPos.col);
    if (dRow !== 0 && dCol !== 0 && dRow !== dCol) return false;

    // 要件: 前後左右斜め6マス以内制限
    const dist = Math.max(dRow, dCol);
    return dist <= 6;
  };

  // 相手最後尾DF (GK除く) のColによる縦オフサイドラインの計算
  const getOffsideCol = (): number | null => {
    const defendingTeam = state.activeTeam === 'TeamA' ? 'TeamB' : 'TeamA';
    const defenders = state.pieces.filter((p) => p.team === defendingTeam && !p.isGoalkeeper);
    if (defenders.length === 0) return null;

    const isAttackingRightward =
      (state.half === 1 && state.activeTeam === 'TeamA') ||
      (state.half === 2 && state.activeTeam === 'TeamB');

    if (isAttackingRightward) {
      return Math.max(...defenders.map((p) => p.position.col));
    } else {
      return Math.min(...defenders.map((p) => p.position.col));
    }
  };

  const offsideCol = getOffsideCol();
  const isAttackingRightward =
    (state.half === 1 && state.activeTeam === 'TeamA') ||
    (state.half === 2 && state.activeTeam === 'TeamB');

  const handleCellClick = (row: number, col: number) => {
    if (!isPlaying) return;
    if (isPassMode) {
      if (isCellValidPass(row, col)) {
        onPassOrShot(row, col);
      }
    } else if (selectedPiece && isCellValidMove(row, col)) {
      onMovePiece(selectedPiece.id, row, col);
    }
  };

  return (
    <div className="pitch-wrapper">
      <div className="pitch-horizontal-arena">
        {/* レーンガイド (左側) */}
        <div className="lane-labels-vertical">
          {lanes.map((lane, idx) => (
            <div key={idx} style={{ height: '64px', display: 'flex', alignItems: 'center' }}>
              {lane}
            </div>
          ))}
        </div>

        {/* 左側ゴール (Col 0, Row 4) */}
        <div
          className={`goal-zone-lateral left ${
            isGoalTargetable({ row: 4, col: 0 }) ? 'targetable' : ''
          }`}
          onClick={() => {
            if (isGoalTargetable({ row: 4, col: 0 })) {
              onPassOrShot(4, 0);
            }
          }}
          title="左ゴール (Goal A)"
        >
          {isGoalTargetable({ row: 4, col: 0 }) ? '⚽ SHOOT' : 'GOAL A'}
        </div>

        {/* 横長サッカーピッチ本体 (縦7行 x 横12列) */}
        <div className="pitch-grid-7x12">
          {/* ハーフウェーライン (縦の中央線) & センターサークル */}
          <div className="pitch-vertical-half-line" />
          <div className="pitch-center-circle" />
          <div className="pitch-center-dot" />

          {/* 縦方向のオフサイドライン */}
          {offsideCol !== null && (
            <div
              className="offside-laser-vertical"
              style={{
                left: `${(offsideCol - (isAttackingRightward ? 0 : 1)) * 68 + 8}px`,
              }}
            >
              <span className="offside-vertical-label">OFFSIDE</span>
            </div>
          )}

          {/* 縦7 x 横12 グリッド描画 */}
          {Array.from({ length: 7 }, (_, r) => r + 1).map((row) =>
            Array.from({ length: 12 }, (_, c) => c + 1).map((col) => {
              const isStripeEven = (row + col) % 2 === 0;
              const validMove = isCellValidMove(row, col);
              const validPass = isCellValidPass(row, col);

              // ペナルティエリア判定 (左: Col 1〜2, Row 3〜5 / 右: Col 11〜12, Row 3〜5)
              const isLeftPA = (col === 1 || col === 2) && row >= 3 && row <= 5;
              const isRightPA = (col === 11 || col === 12) && row >= 3 && row <= 5;
              const paLeftClass = isLeftPA
                ? `penalty-area-left ${row === 3 ? 'pa-top' : ''} ${row === 5 ? 'pa-bottom' : ''} ${col === 1 ? 'pa-left' : ''} ${col === 2 ? 'pa-right' : ''}`
                : '';
              const paRightClass = isRightPA
                ? `penalty-area-right ${row === 3 ? 'pa-top' : ''} ${row === 5 ? 'pa-bottom' : ''} ${col === 11 ? 'pa-left' : ''} ${col === 12 ? 'pa-right' : ''}`
                : '';

              const piecesAtCell = state.pieces.filter(
                (p) => p.position.row === row && p.position.col === col
              );

              const hasLooseBall =
                !state.ball.isHeld &&
                state.ball.position.row === row &&
                state.ball.position.col === col;

              const isOffsideCell =
                offsideCol !== null &&
                (isAttackingRightward ? col > offsideCol : col < offsideCol);

              return (
                <div
                  key={`${row}-${col}`}
                  className={`pitch-cell ${
                    isStripeEven ? 'stripe-even' : 'stripe-odd'
                  } ${validMove ? 'valid-move' : ''} ${validPass ? 'valid-pass' : ''} ${
                    isPassMode && isOffsideCell ? 'is-offside-cell' : ''
                  } ${paLeftClass} ${paRightClass}`}
                  onClick={() => handleCellClick(row, col)}
                >
                  {piecesAtCell.map((piece) => (
                    <PieceToken
                      key={piece.id}
                      piece={piece}
                      isSelected={selectedPieceId === piece.id}
                      hasBall={state.ball.holderPieceId === piece.id}
                      canMove={canPieceMoveClient(piece)}
                      onClick={() => {
                        if (piece.team === state.activeTeam && !isPassMode) {
                          onSelectPiece(piece.id);
                        } else {
                          handleCellClick(row, col);
                        }
                      }}
                    />
                  ))}

                  {hasLooseBall && <div className="loose-ball">⚽</div>}
                </div>
              );
            })
          )}
        </div>

        {/* 右側ゴール (Col 13, Row 4) */}
        <div
          className={`goal-zone-lateral right ${
            isGoalTargetable({ row: 4, col: 13 }) ? 'targetable' : ''
          }`}
          onClick={() => {
            if (isGoalTargetable({ row: 4, col: 13 })) {
              onPassOrShot(4, 13);
            }
          }}
          title="右ゴール (Goal B)"
        >
          {isGoalTargetable({ row: 4, col: 13 }) ? '⚽ SHOOT' : 'GOAL B'}
        </div>
      </div>
    </div>
  );
};
