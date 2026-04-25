import { render, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { GameState } from "../types/game";
import { useGameRealtime } from "./useGameRealtime";

type Handler = (...args: unknown[]) => unknown;

const signalR = vi.hoisted(() => {
  const handlers = new Map<string, Handler>();
  const on = vi.fn((event: string, handler: Handler) => {
    handlers.set(event, handler);
  });
  const invoke = vi.fn(async () => undefined);
  const start = vi.fn(async () => undefined);
  const stop = vi.fn(async () => undefined);

  const connection = {
    on,
    invoke,
    start,
    stop,
  };

  const withUrl = vi.fn(() => builder);
  const withHubProtocol = vi.fn(() => builder);
  const withAutomaticReconnect = vi.fn(() => builder);
  const configureLogging = vi.fn(() => builder);
  const build = vi.fn(() => connection);

  const builder = {
    withUrl,
    withHubProtocol,
    withAutomaticReconnect,
    configureLogging,
    build,
  };

  const HubConnectionBuilder = vi.fn(() => builder);

  return {
    handlers,
    on,
    invoke,
    start,
    stop,
    withUrl,
    withHubProtocol,
    withAutomaticReconnect,
    configureLogging,
    build,
    HubConnectionBuilder,
  };
});

vi.mock("@microsoft/signalr", () => ({
  HubConnectionBuilder: signalR.HubConnectionBuilder,
  HttpTransportType: {
    WebSockets: 1,
  },
  LogLevel: {
    Warning: "warning",
  },
}));

vi.mock("@microsoft/signalr-protocol-msgpack", () => ({
  MessagePackHubProtocol: vi.fn(() => ({ name: "messagepack" })),
}));

function HookHost(props: { sessionCode?: string; onGameState: (state: GameState) => void }) {
  useGameRealtime(props);
  return null;
}

describe("useGameRealtime", () => {
  const gameState: GameState = {
    sessionCode: "ABC123",
    joinPath: "/join/ABC123",
    status: "InProgress",
    boardState: ".........",
    currentTurn: "X",
    winner: null,
    rematchXReady: false,
    rematchOReady: false,
    playerCount: 2,
    players: [],
    lastActivityUtc: "2026-02-14T00:00:00Z",
  };

  beforeEach(() => {
    signalR.handlers.clear();
    signalR.on.mockClear();
    signalR.invoke.mockClear();
    signalR.start.mockClear();
    signalR.stop.mockClear();
    signalR.withUrl.mockClear();
    signalR.withHubProtocol.mockClear();
    signalR.withAutomaticReconnect.mockClear();
    signalR.configureLogging.mockClear();
    signalR.build.mockClear();
    signalR.HubConnectionBuilder.mockClear();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("does nothing when session code is missing", () => {
    render(<HookHost onGameState={vi.fn()} />);

    expect(signalR.HubConnectionBuilder).not.toHaveBeenCalled();
  });

  it("builds SignalR connection and joins session", async () => {
    render(<HookHost onGameState={vi.fn()} sessionCode="ABC123" />);

    await waitFor(() => {
      expect(signalR.start).toHaveBeenCalledTimes(1);
    });

    expect(signalR.withUrl).toHaveBeenCalledWith("/hubs/game", {
      withCredentials: true,
      transport: 1,
      skipNegotiation: true,
    });
    expect(signalR.withHubProtocol).toHaveBeenCalledTimes(1);
    expect(signalR.withAutomaticReconnect).toHaveBeenCalledTimes(1);
    expect(signalR.configureLogging).toHaveBeenCalledWith("warning");
    expect(signalR.on).toHaveBeenCalledWith("GameStateUpdated", expect.any(Function));

    await waitFor(() => {
      expect(signalR.invoke).toHaveBeenCalledWith("JoinSession", "ABC123");
    });
  });

  it("forwards GameStateUpdated events to latest callback", () => {
    const first = vi.fn();
    const second = vi.fn();
    const { rerender } = render(<HookHost onGameState={first} sessionCode="ABC123" />);

    rerender(<HookHost onGameState={second} sessionCode="ABC123" />);

    const updatedHandler = signalR.handlers.get("GameStateUpdated");
    expect(updatedHandler).toBeTruthy();

    updatedHandler?.(gameState);

    expect(first).not.toHaveBeenCalled();
    expect(second).toHaveBeenCalledWith(gameState);
  });

  it("normalizes PascalCase MessagePack payloads", () => {
    const onGameState = vi.fn();
    render(<HookHost onGameState={onGameState} sessionCode="ABC123" />);

    const updatedHandler = signalR.handlers.get("GameStateUpdated");
    expect(updatedHandler).toBeTruthy();

    updatedHandler?.({
      SessionCode: "ABC123",
      JoinPath: "/join/ABC123",
      Status: "InProgress",
      BoardState: ".........",
      CurrentTurn: "X",
      Winner: null,
      RematchXReady: false,
      RematchOReady: false,
      PlayerCount: 1,
      Players: [
        {
          Username: "Alice",
          Mark: "X",
          IsOnline: true,
          IsCurrentPlayer: true,
        },
      ],
      LastActivityUtc: new Date("2026-02-14T00:00:00Z"),
    });

    expect(onGameState).toHaveBeenCalledWith(
      expect.objectContaining({
        sessionCode: "ABC123",
        currentTurn: "X",
        players: [expect.objectContaining({ username: "Alice", isOnline: true })],
        lastActivityUtc: "2026-02-14T00:00:00.000Z",
      }),
    );
  });

  it("only joins session after connection starts", async () => {
    render(<HookHost onGameState={vi.fn()} sessionCode="ABC123" />);

    await waitFor(() => {
      expect(signalR.invoke).toHaveBeenCalledTimes(1);
      expect(signalR.invoke).toHaveBeenCalledWith("JoinSession", "ABC123");
    });
  });

  it("stops the connection on unmount", async () => {
    const { unmount } = render(<HookHost onGameState={vi.fn()} sessionCode="ABC123" />);

    unmount();

    await waitFor(() => {
      expect(signalR.stop).toHaveBeenCalledTimes(1);
    });
  });
});
