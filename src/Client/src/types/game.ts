export type TeamType = 'TeamA' | 'TeamB';

export type GameMode = 'PvP' | 'PvC';

export type GamePhase =
  | 'SetupFirstHalfA'
  | 'SetupFirstHalfB'
  | 'FirstHalf'
  | 'HalfTime'
  | 'SetupSecondHalfA'
  | 'SetupSecondHalfB'
  | 'SecondHalf'
  | 'GameOver';

export interface Position {
  row: number; // 1〜7 (縦7分割)
  col: number; // 1〜12 (横12マス)
}

export interface Piece {
  id: string;
  team: TeamType;
  number: number;
  name: string;
  ability: number; // 1, 2, 3
  isGoalkeeper: boolean;
  position: Position;
  kickoffPosition?: Position;
}

export interface Ball {
  position: Position;
  holderPieceId: string | null;
  isHeld: boolean;
}

export interface TurnActionState {
  maxStandardMoveCount: number;
  standardMoveCount?: number;
  movedPieceIds: string[];
  maxPassOrShotCount: number;
  passOrShotCount: number;
  hasPassedOrShot: boolean;
  remainingPassOrShots: number;
  canPassOrShot: boolean;
  gkBonusAvailable: boolean;
  hasUsedGkBonusMove: boolean;
  standardMovesRemaining: number;
  tackledPieceIds?: string[];
}

export type DuelType = 'Tackle' | 'Intercept' | 'Shot';

export interface DuelParticipant {
  pieceId: string;
  name: string;
  number: number;
  ability: number;
  abilityBonus: number;
  totalAbility: number;
  isGoalkeeper: boolean;
}

export interface DuelContext {
  duelId: string;
  type: DuelType;
  duelPosition: Position;
  passTargetPosition?: Position;
  attackingTeam: TeamType;
  defendingTeam: TeamType;
  attackers: DuelParticipant[];
  defenders: DuelParticipant[];
  attackerAbilitySum: number;
  defenderAbilitySum: number;
  attackerDice: number | null;
  defenderDice: number | null;
  attackerTotal: number;
  defenderTotal: number;
  winner: TeamType | null;
  isResolved: boolean;
  message: string;
  hasGkHandBonus?: boolean;
}

export interface GameState {
  gameId: string;
  phase: GamePhase;
  mode: GameMode;
  activeTeam: TeamType;
  half: number;
  turn: number;
  isAdditionalTime: boolean;
  additionalTimeTotal: number;
  additionalTimeTurnsElapsed: number;
  scoreTeamA: number;
  scoreTeamB: number;
  pieces: Piece[];
  ball: Ball;
  currentTurnAction: TurnActionState;
  pendingDuel: DuelContext | null;
  lastResolvedDuel?: DuelContext | null;
  matchLogs: string[];
  offsideWarning: string | null;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
}

export interface PiecePlacementDto {
  pieceId: string;
  row: number;
  col: number;
  ability?: number;
}

export interface RelativePlacement {
  number: number;
  positionName: string;
  row: number;
  relativeCol: number;
  defaultAbility: number;
}

export interface FormationPreset {
  id: string;
  name: string;
  category: string;
  description: string;
  positions: RelativePlacement[];
}

