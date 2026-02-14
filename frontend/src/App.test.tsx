import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { App } from "./App";

vi.mock("./pages/LobbyPage", () => ({
  LobbyPage: () => <div>Lobby View</div>,
}));

vi.mock("./pages/GamePage", () => ({
  GamePage: () => <div>Game View</div>,
}));

function renderApp(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  );
}

describe("App", () => {
  it("renders lobby for root route", () => {
    renderApp("/");

    expect(screen.getByText("Lobby View")).toBeInTheDocument();
  });

  it("renders lobby for join route", () => {
    renderApp("/join/abc123");

    expect(screen.getByText("Lobby View")).toBeInTheDocument();
  });

  it("renders game page for game route", () => {
    renderApp("/game/abc123");

    expect(screen.getByText("Game View")).toBeInTheDocument();
  });

  it("redirects unknown routes back to lobby", () => {
    renderApp("/unknown/route");

    expect(screen.getByText("Lobby View")).toBeInTheDocument();
  });
});
