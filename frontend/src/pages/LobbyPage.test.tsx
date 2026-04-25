import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useParams } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LobbyPage } from "./LobbyPage";

const hooks = vi.hoisted(() => ({
  useStartGameMutation: vi.fn(),
  useJoinGameMutation: vi.fn(),
}));

vi.mock("../store/services/gameApi", () => ({
  useStartGameMutation: hooks.useStartGameMutation,
  useJoinGameMutation: hooks.useJoinGameMutation,
}));

function GameRouteProbe() {
  const { code } = useParams<{ code: string }>();
  return <p>Game route {code}</p>;
}

function renderLobby(initialPath = "/") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route element={<LobbyPage />} path="/" />
        <Route element={<LobbyPage />} path="/join/:code" />
        <Route element={<GameRouteProbe />} path="/game/:code" />
      </Routes>
    </MemoryRouter>,
  );
}

describe("LobbyPage", () => {
  const startTrigger = vi.fn();
  const joinTrigger = vi.fn();

  beforeEach(() => {
    startTrigger.mockReset();
    joinTrigger.mockReset();

    hooks.useStartGameMutation.mockReturnValue([startTrigger, { isLoading: false }]);
    hooks.useJoinGameMutation.mockReturnValue([joinTrigger, { isLoading: false }]);
  });

  it("shows validation when starting with empty username", async () => {
    renderLobby();

    fireEvent.click(screen.getByRole("button", { name: "Start Game" }));

    expect(await screen.findByText("Username is required")).toBeInTheDocument();
    expect(startTrigger).not.toHaveBeenCalled();
  });

  it("shows validation when joining with an invalid session code", async () => {
    renderLobby();

    fireEvent.change(screen.getByLabelText("Session Code"), {
      target: { value: "abc" },
    });
    fireEvent.change(screen.getByLabelText("Username", { selector: "#join-username" }), {
      target: { value: "Bob" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Join Game" }));

    expect(await screen.findByText("Session code must be 8 letters/numbers")).toBeInTheDocument();
    expect(joinTrigger).not.toHaveBeenCalled();
  });

  it("prefills join code from /join route param", () => {
    renderLobby("/join/a1b2c3d4");

    expect(screen.getByLabelText("Session Code")).toHaveValue("a1b2c3d4");
  });

  it("starts a game, trims username, and navigates to game route", async () => {
    startTrigger.mockReturnValue({
      unwrap: vi.fn().mockResolvedValue({ sessionCode: "ZXCVBN12" }),
    });

    renderLobby();

    fireEvent.change(screen.getByLabelText("Username", { selector: "#start-username" }), {
      target: { value: "  Alice  " },
    });
    fireEvent.click(screen.getByRole("button", { name: "Start Game" }));

    await waitFor(() => {
      expect(startTrigger).toHaveBeenCalledWith({ username: "Alice" });
    });

    expect(await screen.findByText("Game route ZXCVBN12")).toBeInTheDocument();
  });

  it("joins a game, normalizes payload, and navigates to game route", async () => {
    joinTrigger.mockReturnValue({
      unwrap: vi.fn().mockResolvedValue({ sessionCode: "QW12ER34" }),
    });

    renderLobby();

    fireEvent.change(screen.getByLabelText("Session Code"), {
      target: { value: "qw12er34" },
    });
    fireEvent.change(screen.getByLabelText("Username", { selector: "#join-username" }), {
      target: { value: "  Bob  " },
    });
    fireEvent.click(screen.getByRole("button", { name: "Join Game" }));

    await waitFor(() => {
      expect(joinTrigger).toHaveBeenCalledWith({ sessionCode: "QW12ER34", username: "Bob" });
    });

    expect(await screen.findByText("Game route QW12ER34")).toBeInTheDocument();
  });

  it("shows loading states for start and join buttons", () => {
    hooks.useStartGameMutation.mockReturnValue([startTrigger, { isLoading: true }]);
    hooks.useJoinGameMutation.mockReturnValue([joinTrigger, { isLoading: true }]);

    renderLobby();

    expect(screen.getByRole("button", { name: "Starting..." })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Joining..." })).toBeDisabled();
  });
});
