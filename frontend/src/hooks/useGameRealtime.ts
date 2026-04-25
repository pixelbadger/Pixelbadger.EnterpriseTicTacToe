import { useEffect, useRef } from "react";
import { HubConnection, HubConnectionBuilder, HttpTransportType, LogLevel } from "@microsoft/signalr";
import { MessagePackHubProtocol } from "@microsoft/signalr-protocol-msgpack";
import type { GameState } from "../types/game";

type Options = {
  sessionCode?: string;
  onGameState: (state: GameState) => void;
};

export function useGameRealtime({ sessionCode, onGameState }: Options) {
  const connectionRef = useRef<HubConnection | null>(null);
  const onGameStateRef = useRef(onGameState);

  useEffect(() => {
    onGameStateRef.current = onGameState;
  }, [onGameState]);

  useEffect(() => {
    if (!sessionCode) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl("/hubs/game", {
        withCredentials: true,
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .withHubProtocol(new MessagePackHubProtocol())
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = connection;
    connection.on("GameStateUpdated", (state: unknown) => onGameStateRef.current(normalizeGameState(state)));

    void connection
      .start()
      .then(async () => {
        await connection.invoke("JoinSession", sessionCode);
      })
      .catch((error) => {
        console.error("SignalR connection failed", error);
      });

    return () => {
      connectionRef.current = null;
      void connection.stop();
    };
  }, [sessionCode]);
}

function normalizeGameState(state: unknown): GameState {
  const players = readArray(state, "players", "Players").map((player) => ({
    username: readString(player, "username", "Username"),
    mark: readString(player, "mark", "Mark") as "X" | "O",
    isOnline: readBoolean(player, "isOnline", "IsOnline"),
    isCurrentPlayer: readBoolean(player, "isCurrentPlayer", "IsCurrentPlayer"),
  }));

  const lastActivityUtc = readValue(state, "lastActivityUtc", "LastActivityUtc");

  return {
    sessionCode: readString(state, "sessionCode", "SessionCode"),
    joinPath: readString(state, "joinPath", "JoinPath"),
    status: readString(state, "status", "Status") as GameState["status"],
    boardState: readString(state, "boardState", "BoardState"),
    currentTurn: readNullableMark(state, "currentTurn", "CurrentTurn"),
    winner: readNullableMark(state, "winner", "Winner"),
    rematchXReady: readBoolean(state, "rematchXReady", "RematchXReady"),
    rematchOReady: readBoolean(state, "rematchOReady", "RematchOReady"),
    playerCount: readNumber(state, "playerCount", "PlayerCount"),
    players,
    lastActivityUtc: lastActivityUtc instanceof Date ? lastActivityUtc.toISOString() : String(lastActivityUtc),
  };
}

function readValue(source: unknown, camelKey: string, pascalKey: string): unknown {
  const record = source as Record<string, unknown>;
  return record[camelKey] ?? record[pascalKey];
}

function readString(source: unknown, camelKey: string, pascalKey: string): string {
  return String(readValue(source, camelKey, pascalKey));
}

function readBoolean(source: unknown, camelKey: string, pascalKey: string): boolean {
  return Boolean(readValue(source, camelKey, pascalKey));
}

function readNumber(source: unknown, camelKey: string, pascalKey: string): number {
  return Number(readValue(source, camelKey, pascalKey));
}

function readArray(source: unknown, camelKey: string, pascalKey: string): unknown[] {
  const value = readValue(source, camelKey, pascalKey);
  return Array.isArray(value) ? value : [];
}

function readNullableMark(source: unknown, camelKey: string, pascalKey: string): "X" | "O" | null {
  const value = readValue(source, camelKey, pascalKey);
  return value === "X" || value === "O" ? value : null;
}
