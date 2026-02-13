export type PlayerState = {
  username: string;
  mark: "X" | "O";
  isOnline: boolean;
  isCurrentPlayer: boolean;
};

export type GameState = {
  sessionCode: string;
  joinPath: string;
  status: "WaitingForOpponent" | "InProgress" | "Won" | "Draw" | "Expired";
  boardState: string;
  currentTurn: "X" | "O" | null;
  winner: "X" | "O" | null;
  rematchXReady: boolean;
  rematchOReady: boolean;
  playerCount: number;
  players: PlayerState[];
  lastActivityUtc: string;
};

export type StartGameRequest = {
  username: string;
};

export type JoinGameRequest = {
  sessionCode: string;
  username: string;
};

export type MoveRequest = {
  cellIndex: number;
};
