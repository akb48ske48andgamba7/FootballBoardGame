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

export const Board: React.FC<BoardProps> = ({
  state,
  selectedPieceId,
  isPassMode,
  onSelectPiece,
  onMovePiece,
  onPassOrShot,
}) => {
  const lanes = ['左サイド', '左ハーフ', 'センター', '右ハーフ', '右サイド'];

  const selectedPiece = state.pieces.find((p) => p.id === selectedPieceId);
  const ballHolder = state.pieces.find((p) => p.id === state.ball.holderPieceId);

  const canPieceMoveClient = (piece: Piece): boolean => {
    if (piece.team !== state.activeTeam) return false;
    const action = state.currentTurnAction;
    const moved = action?.movedPieceIds || [];
    const hasMoved = moved.includes(piece.id);

    if (hasMoved) {
      return piece.isGoalkeeper && action.gkBonusAvailable && !action.hasUsedGkBonusMove;
    }
    if (action?.standardMovesRemaining > 0) {
      return true;
    }
    return piece.isGoalkeeper && action.gkBonusAvailable && !action.hasUsedGkBonusMove;
  };

  // 攻撃目標ゴール
  const targetGoalPos: Position =
    (state.half === 1 && state.activeTeam === 'TeamA') ||
    (state.half === 2 && state.activeTeam === 'TeamB')
      ? { row: 11, col: 3 }
      : { row: 0, col: 3 };

  // 移動可能マスの計算 (最大2マス)
  const isCellValidMove = (row: number, col: number): boolean => {
    if (isPassMode || !selectedPiece) return false;
    const dRow = Math.abs(selectedPiece.position.row - row);
    const dCol = Math.abs(selectedPiece.position.col - col);
    const dist = Math.max(dRow, dCol);
    return dist >= 1 && dist <= 2;
  };

  // パス/シュート可能マスの計算 (縦・横・斜めの直線)
  const isCellValidPass = (row: number, col: number): boolean => {
    if (!isPassMode || !ballHolder) return false;
    const startRow = ballHolder.position.row;
    const startCol = ballHolder.position.col;
    if (startRow === row && startCol === col) return false;

    const dRow = Math.abs(startRow - row);
    const dCol = Math.abs(startCol - col);
    return dRow === 0 || dCol === 0 || dRow === dCol;
  };

  // シュート可能な直線上のゴールかどうかの判定
  const isGoalTargetable = (goalPos: Position): boolean => {
    if (!isPassMode || !ballHolder) return false;
    if (goalPos.row !== targetGoalPos.row || goalPos.col !== targetGoalPos.col) return false;

    const startRow = ballHolder.position.row;
    const startCol = ballHolder.position.col;
    const dRow = Math.abs(startRow - goalPos.row);
    const dCol = Math.abs(startCol - goalPos.col);
    return dRow === 0 || dCol === 0 || dRow === dCol;
  };

  // 相手最後尾DF (GK除く) のRowによるオフサイドラインの計算
  const getOffsideRow = (): number | null => {
    const defendingTeam = state.activeTeam === 'TeamA' ? 'TeamB' : 'TeamA';
    const defenders = state.pieces.filter((p) => p.team === defendingTeam && !p.isGoalkeeper);
    if (defenders.length === 0) return null;

    const isAttackingDownward =
      (state.half === 1 && state.activeTeam === 'TeamA') ||
      (state.half === 2 && state.activeTeam === 'TeamB');

    if (isAttackingDownward) {
      return Math.max(...defenders.map((p) => p.position.row));
    } else {
      return Math.min(...defenders.map((p) => p.position.row));
    }
  };

  const offsideRow = getOffsideRow();
  const isAttackingDownward =
    (state.half === 1 && state.activeTeam === 'TeamA') ||
    (state.half === 2 && state.activeTeam === 'TeamB');

  const handleCellClick = (row: number, col: number) => {
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
      {/* 5レーンヘッダー */}
      <div className="lane-headers">
        {lanes.map((lane, idx) => (
          <div key={idx}>{lane}</div>
        ))}
      </div>

      {/* 上側ゴール (Row 0, Col 3) */}
      <div
        className={`goal-zone top ${isGoalTargetable({ row: 0, col: 3 }) ? 'targetable' : ''}`}
        onClick={() => {
          if (isGoalTargetable({ row: 0, col: 3 })) {
            onPassOrShot(0, 3);
          }
        }}
      >
        {isGoalTargetable({ row: 0, col: 3 }) ? '⚽ SHOOT!' : 'GOAL A'}
      </div>

      {/* サッカーピッチ (10x5) */}
      <div className="pitch-grid">
        {/* ハーフウェーライン & センターサークル */}
        <div className="pitch-half-line" />
        <div className="pitch-center-circle" />
        <div className="pitch-center-dot" />

        {/* オフサイドライン表示 */}
        {offsideRow !== null && (
          <div
            className="offside-laser-line"
            style={{
              // Row 1〜10 の位置 (ピッチの高さ68px * 10 = 680px + gap)
              top: `${(offsideRow - (isAttackingDownward ? 0 : 1)) * 72 + 8}px`,
            }}
          >
            <span className="offside-label">OFFSIDE LINE</span>
          </div>
        )}

        {/* 10 x 5 グリッド描画 */}
        {Array.from({ length: 10 }, (_, r) => r + 1).map((row) =>
          Array.from({ length: 5 }, (_, c) => c + 1).map((col) => {
            const isStripeEven = (row + col) % 2 === 0;
            const validMove = isCellValidMove(row, col);
            const validPass = isCellValidPass(row, col);

            // このマスにいる選手たち
            const piecesAtCell = state.pieces.filter(
              (p) => p.position.row === row && p.position.col === col
            );

            // ルーズボール (誰も保持していないボールがこのマスにあるか)
            const hasLooseBall =
              !state.ball.isHeld &&
              state.ball.position.row === row &&
              state.ball.position.col === col;

            // オフサイドゾーンか
            const isOffsideCell =
              offsideRow !== null &&
              (isAttackingDownward ? row > offsideRow : row < offsideRow);

            return (
              <div
                key={`${row}-${col}`}
                className={`pitch-cell ${
                  isStripeEven ? 'stripe-even' : 'stripe-odd'
                } ${validMove ? 'valid-move' : ''} ${validPass ? 'valid-pass' : ''} ${
                  isPassMode && isOffsideCell ? 'is-offside-cell' : ''
                }`}
                onClick={() => handleCellClick(row, col)}
              >
                {/* コマの描画 */}
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

                {/* ルーズボール */}
                {hasLooseBall && <div className="loose-ball">⚽</div>}
              </div>
            );
          })
        )}
      </div>

      {/* 下側ゴール (Row 11, Col 3) */}
      <div
        className={`goal-zone bottom ${isGoalTargetable({ row: 11, col: 3 }) ? 'targetable' : ''}`}
        onClick={() => {
          if (isGoalTargetable({ row: 11, col: 3 })) {
            onPassOrShot(11, 3);
          }
        }}
      >
        {isGoalTargetable({ row: 11, col: 3 }) ? '⚽ SHOOT!' : 'GOAL B'}
      </div>
    </div>
  );
};
