import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { GameState } from "../types/game";
import { GamePage } from "./GamePage";

const hooks = vi.hoisted(() => ({
  useGameRealtime: vi.fn(),
  useGetGameStateQuery: vi.fn(),
  useMakeMoveMutation: vi.fn(),
  useRequestRematchMutation: vi.fn(),
}));

vi.mock("../hooks/useGameRealtime", () => ({
  useGameRealtime: hooks.useGameRealtime,
}));

vi.mock("../store/services/gameApi", () => ({
  useGetGameStateQuery: hooks.useGetGameStateQuery,
  useMakeMoveMutation: hooks.useMakeMoveMutation,
  useRequestRematchMutation: hooks.useRequestRematchMutation,
}));

const baseGame: GameState = {
  sessionCode: "ABC123",
  joinPath: "/join/ABC123",
  status: "InProgress",
  boardState: ".........",
  currentTurn: "X",
  winner: null,
  rematchXReady: false,
  rematchOReady: false,
  playerCount: 2,
  players: [
    { username: "Alice", mark: "X", isOnline: true, isCurrentPlayer: true },
    { username: "Bob", mark: "O", isOnline: false, isCurrentPlayer: false },
  ],
  lastActivityUtc: "2026-02-14T00:00:00Z",
};

function renderGame(path = "/game/abc123") {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route element={<GamePage />} path="/" />
        <Route element={<GamePage />} path="/game/:code" />
      </Routes>
    </MemoryRouter>,
  );
}

describe("GamePage", () => {
  const refetch = vi.fn();
  const makeMove = vi.fn();
  const requestRematch = vi.fn();

  beforeEach(() => {
    refetch.mockReset();
    makeMove.mockReset();
    requestRematch.mockReset();

    hooks.useGameRealtime.mockReset();
    hooks.useGameRealtime.mockImplementation(() => undefined);

    hooks.useGetGameStateQuery.mockReset();
    hooks.useGetGameStateQuery.mockReturnValue({
      data: baseGame,
      isLoading: false,
      isError: false,
      refetch,
    });

    hooks.useMakeMoveMutation.mockReset();
    hooks.useMakeMoveMutation.mockReturnValue([makeMove, { isLoading: false }]);

    hooks.useRequestRematchMutation.mockReset();
    hooks.useRequestRematchMutation.mockReturnValue([requestRematch, { isLoading: false }]);
  });

  it("renders invalid session route when code is missing", () => {
    renderGame("/");

    expect(screen.getByText("Invalid session route")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Back to lobby" })).toHaveAttribute("href", "/");
  });

  it("renders loading state when game query is loading", () => {
    hooks.useGetGameStateQuery.mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
      refetch,
    });

    renderGame();

    expect(screen.getByText("Loading game...")).toBeInTheDocument();
  });

  it("renders error state and retries query", () => {
    hooks.useGetGameStateQuery.mockReturnValue({
      data: baseGame,
      isLoading: false,
      isError: true,
      refetch,
    });

    renderGame();

    expect(screen.getByText("Unable to load session")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Retry" }));
    expect(refetch).toHaveBeenCalledTimes(1);
  });

  it("renders status-specific messaging", () => {
    hooks.useGetGameStateQuery.mockReturnValue({
      data: { ...baseGame, status: "WaitingForOpponent", currentTurn: null },
      isLoading: false,
      isError: false,
      refetch,
    });

    renderGame();
    expect(screen.getByText("Waiting for opponent to join.")).toBeInTheDocument();
  });

  it("renders winner and draw messaging", () => {
    hooks.useGetGameStateQuery.mockReturnValue({
      data: { ...baseGame, status: "Won", winner: "X", boardState: "XXXOO...." },
      isLoading: false,
      isError: false,
      refetch,
    });

    const { rerender } = renderGame();

    expect(screen.getByText("Winner: X")).toBeInTheDocument();

    hooks.useGetGameStateQuery.mockReturnValue({
      data: { ...baseGame, status: "Draw", winner: null, boardState: "XOXOOXXXO" },
      isLoading: false,
      isError: false,
      refetch,
    });

    rerender(
      <MemoryRouter initialEntries={["/game/abc123"]}>
        <Routes>
          <Route element={<GamePage />} path="/game/:code" />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText("Draw - both players may request a rematch.")).toBeInTheDocument();
  });

  it("sends move mutations for playable cells", async () => {
    makeMove.mockReturnValue({
      unwrap: vi.fn().mockResolvedValue({
        ...baseGame,
        boardState: "X........",
        currentTurn: "O",
      }),
    });

    const { container } = renderGame();
    const boardButtons = container.querySelectorAll("button.aspect-square");

    expect(boardButtons).toHaveLength(9);
    expect(boardButtons[0]).toBeEnabled();

    fireEvent.click(boardButtons[0]);

    await waitFor(() => {
      expect(makeMove).toHaveBeenCalledWith({
        sessionCode: "ABC123",
        body: { cellIndex: 0 },
      });
    });
  });

  it("disables occupied cells and disables all cells when current player cannot play", () => {
    hooks.useGetGameStateQuery.mockReturnValue({
      data: {
        ...baseGame,
        boardState: "X........",
      },
      isLoading: false,
      isError: false,
      refetch,
    });

    const { container, rerender } = renderGame();
    let boardButtons = Array.from(container.querySelectorAll<HTMLButtonElement>("button.aspect-square"));

    expect(boardButtons[0]).toBeDisabled();
    expect(boardButtons[1]).toBeEnabled();

    hooks.useGetGameStateQuery.mockReturnValue({
      data: {
        ...baseGame,
        currentTurn: "O",
      },
      isLoading: false,
      isError: false,
      refetch,
    });

    rerender(
      <MemoryRouter initialEntries={["/game/abc123"]}>
        <Routes>
          <Route element={<GamePage />} path="/game/:code" />
        </Routes>
      </MemoryRouter>,
    );

    boardButtons = Array.from(container.querySelectorAll<HTMLButtonElement>("button.aspect-square"));
    expect(boardButtons.every((button) => button.disabled)).toBe(true);
  });

  it("requests rematch when game is won or drawn", async () => {
    hooks.useGetGameStateQuery.mockReturnValue({
      data: {
        ...baseGame,
        status: "Won",
        winner: "X",
      },
      isLoading: false,
      isError: false,
      refetch,
    });
    requestRematch.mockReturnValue({
      unwrap: vi.fn().mockResolvedValue({ ...baseGame, status: "InProgress" }),
    });

    renderGame();

    fireEvent.click(screen.getByRole("button", { name: "Request Rematch" }));

    await waitFor(() => {
      expect(requestRematch).toHaveBeenCalledWith("ABC123");
    });
  });

  it("renders players, share link, and copies link", async () => {
    const writeText = vi.mocked(navigator.clipboard.writeText);

    renderGame();

    expect(screen.getByText("X - Alice (You)")).toBeInTheDocument();
    expect(screen.getByText("O - Bob")).toBeInTheDocument();
    expect(screen.getByText("Online")).toBeInTheDocument();
    expect(screen.getByText("Offline")).toBeInTheDocument();

    const expectedLink = `${window.location.origin}/join/ABC123`;
    expect(screen.getByText(expectedLink)).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Copy Link" }));

    await waitFor(() => {
      expect(writeText).toHaveBeenCalledWith(expectedLink);
    });
  });

  it("subscribes realtime with uppercased session code", () => {
    renderGame("/game/ab12cd");

    expect(hooks.useGameRealtime).toHaveBeenCalledWith(
      expect.objectContaining({
        sessionCode: "AB12CD",
        onGameState: expect.any(Function),
      }),
    );
  });
});
