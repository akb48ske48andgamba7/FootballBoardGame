import type { ApiResponse, GameMode, GameState, PiecePlacementDto, TeamType } from '../types/game';

const API_BASE = '/api/game';

async function request<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${endpoint}`, {
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
    ...options,
  });

  const json: ApiResponse<T> = await res.json();
  if (!res.ok || !json.success || !json.data) {
    throw new Error(json.message || 'APIリクエストに失敗しました。');
  }
  return json.data;
}

export const api = {
  getGameState: () => request<GameState>(''),
  resetGame: () => request<GameState>('/reset', { method: 'POST' }),
  setGameMode: (mode: GameMode) =>
    request<GameState>('/mode', {
      method: 'POST',
      body: JSON.stringify({ mode }),
    }),
  executeCpuStep: () =>
    request<GameState>('/cpu/step', {
      method: 'POST',
    }),
  applyDefaultFormation: (team: TeamType) =>
    request<GameState>(`/setup/default?team=${team}`, { method: 'POST' }),
  setupTeam: (team: TeamType, placements: PiecePlacementDto[]) =>
    request<GameState>(`/setup?team=${team}`, {
      method: 'POST',
      body: JSON.stringify({ placements }),
    }),
  movePiece: (pieceId: string, targetRow: number, targetCol: number) =>
    request<GameState>('/move', {
      method: 'POST',
      body: JSON.stringify({ pieceId, targetRow, targetCol }),
    }),
  passOrShot: (targetRow: number, targetCol: number) =>
    request<GameState>('/pass', {
      method: 'POST',
      body: JSON.stringify({ targetRow, targetCol }),
    }),
  resolveDuel: () => request<GameState>('/duel/resolve', { method: 'POST' }),
  endTurn: () => request<GameState>('/end-turn', { method: 'POST' }),
};
