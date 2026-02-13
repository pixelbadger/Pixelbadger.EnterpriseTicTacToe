import { useEffect, useRef } from "react";
import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
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
      .withUrl("/hubs/game", { withCredentials: true })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = connection;
    connection.on("GameStateUpdated", (state: GameState) => onGameStateRef.current(state));

    void connection
      .start()
      .then(async () => {
        await connection.invoke("JoinSession", sessionCode);
        await connection.invoke("RefreshState", sessionCode);
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
