import React from 'react';
import type { Piece } from '../types/game';

interface PieceTokenProps {
  piece: Piece;
  isSelected: boolean;
  hasBall: boolean;
  canMove: boolean;
  onClick: () => void;
}

export const PieceToken: React.FC<PieceTokenProps> = ({
  piece,
  isSelected,
  hasBall,
  canMove,
  onClick,
}) => {
  const getStars = (ability: number) => {
    switch (ability) {
      case 3:
        return '★★★';
      case 2:
        return '★★';
      default:
        return '★';
    }
  };

  return (
    <div
      className={`piece-token team-${piece.team === 'TeamA' ? 'A' : 'B'} ability-${piece.ability} ${
        piece.isGoalkeeper ? 'is-gk' : ''
      } ${isSelected ? 'selected' : ''} ${hasBall ? 'has-ball' : ''}`}
      onClick={(e) => {
        e.stopPropagation();
        onClick();
      }}
      title={`${piece.team} No.${piece.number} (${piece.name}) - 能力: ${piece.ability}${
        piece.isGoalkeeper ? ' [GK]' : ''
      }${hasBall ? ' [ボール保持]' : ''}${canMove ? ' [移動可能]' : ''}`}
      style={{
        cursor: canMove || hasBall ? 'pointer' : 'default',
        opacity: canMove || hasBall || isSelected ? 1 : 0.85,
      }}
    >
      {/* GK アイコン */}
      {piece.isGoalkeeper && <span className="gk-badge-icon">🧤</span>}

      {/* 背番号 */}
      <span className="piece-number">{piece.number}</span>

      {/* 能力値の星 */}
      <span className="piece-stars">{getStars(piece.ability)}</span>

      {/* ボール保持バッジ */}
      {hasBall && <span className="ball-indicator-icon">⚽</span>}
    </div>
  );
};
