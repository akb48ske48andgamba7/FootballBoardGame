import React from 'react';
import type { GameMode } from '../types/game';
import { RotateCcw, BookOpen, Users, Bot } from 'lucide-react';

interface HeaderProps {
  mode: GameMode;
  onSelectMode: (mode: GameMode) => void;
  onReset: () => void;
  onOpenRules: () => void;
}

export const Header: React.FC<HeaderProps> = ({
  mode,
  onSelectMode,
  onReset,
  onOpenRules,
}) => {
  return (
    <header className="game-header">
      <div className="game-title">
        <span style={{ fontSize: '26px' }}>⚽</span>
        <span>POSITIONAL TACTICS</span>
        <span style={{ fontSize: '12px', color: 'var(--text-muted)', fontWeight: 600 }}>
          7×10 Pitch
        </span>
      </div>

      <div className="header-actions">
        {/* 対戦モード切替トグル */}
        <div className="mode-toggle-group">
          <button
            className={`mode-btn ${mode === 'PvC' ? 'active' : ''}`}
            onClick={() => onSelectMode('PvC')}
            title="1人でCPUと対戦"
          >
            <Bot size={15} />
            <span>vs CPU</span>
          </button>
          <button
            className={`mode-btn ${mode === 'PvP' ? 'active' : ''}`}
            onClick={() => onSelectMode('PvP')}
            title="1台の端末で2人対戦"
          >
            <Users size={15} />
            <span>2人対戦</span>
          </button>
        </div>

        <button className="btn-secondary" onClick={onOpenRules}>
          <BookOpen size={16} />
          <span>ルール</span>
        </button>
        <button className="btn-secondary" onClick={onReset}>
          <RotateCcw size={16} />
          <span>リセット</span>
        </button>
      </div>
    </header>
  );
};
