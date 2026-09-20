import React from 'react';
import { RotateCcw, BookOpen } from 'lucide-react';

interface HeaderProps {
  onReset: () => void;
  onOpenRules: () => void;
}

export const Header: React.FC<HeaderProps> = ({ onReset, onOpenRules }) => {
  return (
    <header className="game-header">
      <div className="game-title">
        <span style={{ fontSize: '26px' }}>⚽</span>
        <span>POSITIONAL TACTICS</span>
        <span style={{ fontSize: '12px', color: 'var(--text-muted)', fontWeight: 600 }}>
          Football Board Game
        </span>
      </div>

      <div className="header-actions">
        <button className="btn-secondary" onClick={onOpenRules}>
          <BookOpen size={16} />
          <span>ルール説明</span>
        </button>
        <button className="btn-secondary" onClick={onReset}>
          <RotateCcw size={16} />
          <span>リセット</span>
        </button>
      </div>
    </header>
  );
};
