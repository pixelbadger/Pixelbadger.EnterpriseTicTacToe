import { useMemo, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import { Button } from "../components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../components/ui/card";
import { useGameRealtime } from "../hooks/useGameRealtime";
import {
  useGetGameStateQuery,
  useMakeMoveMutation,
  useRequestRematchMutation,
} from "../store/services/gameApi";
import type { GameState } from "../types/game";

function prettyStatus(game: GameState) {
  if (game.status === "Won") {
    return `Winner: ${game.winner}`;
  }

  if (game.status === "Draw") {
    return "Draw - both players may request a rematch.";
  }

  if (game.status === "WaitingForOpponent") {
    return "Waiting for opponent to join.";
  }

  if (game.status === "Expired") {
    return "Session expired due to inactivity.";
  }

  return `Turn: ${game.currentTurn}`;
}

export function GamePage() {
  const { code } = useParams<{ code: string }>();
  const sessionCode = code?.toUpperCase();
  const location = useLocation();
  const [liveGame, setLiveGame] = useState<GameState | null>(() => {
    const routeState = location.state as { gameState?: GameState } | null;
    return sessionCode != null && routeState?.gameState?.sessionCode === sessionCode ? routeState.gameState : null;
  });
  const { data: fetchedGame, isLoading, isError, refetch } = useGetGameStateQuery(sessionCode ?? "", {
    skip: !sessionCode || liveGame !== null,
  });
  const [makeMove, { isLoading: isMoveLoading }] = useMakeMoveMutation();
  const [requestRematch, { isLoading: isRematchLoading }] = useRequestRematchMutation();

  useGameRealtime({
    sessionCode,
    onGameState: (nextState) => {
      setLiveGame(nextState);
    },
  });

  const game = liveGame ?? fetchedGame;
  const currentPlayer = game?.players.find((player) => player.isCurrentPlayer);
  const canPlay = game?.status === "InProgress" && currentPlayer?.mark === game.currentTurn;

  const shareLink = useMemo(() => {
    if (!game) {
      return "";
    }

    return `${window.location.origin}/join/${game.sessionCode}`;
  }, [game]);

  if (!sessionCode) {
    return (
      <main className="page-shell">
        <Card className="mx-auto mt-12 max-w-xl">
          <CardHeader>
            <CardTitle>Invalid session route</CardTitle>
          </CardHeader>
          <CardContent>
            <Link className="text-sm text-accent underline" to="/">
              Back to lobby
            </Link>
          </CardContent>
        </Card>
      </main>
    );
  }

  if (isLoading || !game) {
    return <main className="page-shell">Loading game...</main>;
  }

  if (isError) {
    return (
      <main className="page-shell">
        <Card className="mx-auto mt-12 max-w-xl">
          <CardHeader>
            <CardTitle>Unable to load session</CardTitle>
            <CardDescription>This game may be full, expired, or inaccessible from this cookie identity.</CardDescription>
          </CardHeader>
          <CardContent className="flex gap-3">
            <Button onClick={() => refetch()} variant="secondary">
              Retry
            </Button>
            <Link className="text-sm text-accent underline" to="/">
              Back to lobby
            </Link>
          </CardContent>
        </Card>
      </main>
    );
  }

  return (
    <main className="page-shell px-4 py-8">
      <section className="mx-auto grid w-full max-w-6xl gap-6 lg:grid-cols-[2fr_1fr]">
        <Card>
          <CardHeader>
            <CardTitle>Session {game.sessionCode}</CardTitle>
            <CardDescription>{prettyStatus(game)}</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-3 gap-3">
              {game.boardState.split("").map((value, index) => {
                const display = value === "." ? "" : value;
                const isOccupied = value !== ".";

                return (
                  <button
                    className="aspect-square rounded-lg border border-white/20 bg-slate-950/70 text-4xl font-display font-bold text-white hover:bg-slate-900/80 disabled:cursor-not-allowed disabled:opacity-70"
                    disabled={Boolean(isOccupied || !canPlay || isMoveLoading)}
                    key={`${game.sessionCode}-${index}`}
                    onClick={async () => {
                      const updated = await makeMove({
                        sessionCode: game.sessionCode,
                        body: { cellIndex: index },
                      }).unwrap();
                      setLiveGame(updated);
                    }}
                    type="button"
                  >
                    {display}
                  </button>
                );
              })}
            </div>

            {(game.status === "Won" || game.status === "Draw") && (
              <div className="mt-6">
                <Button
                  disabled={isRematchLoading}
                  onClick={async () => {
                    const updated = await requestRematch(game.sessionCode).unwrap();
                    setLiveGame(updated);
                  }}
                  type="button"
                  variant="secondary"
                >
                  {isRematchLoading ? "Sending rematch vote..." : "Request Rematch"}
                </Button>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Players</CardTitle>
            <CardDescription>Only two players are allowed in this session.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {game.players.map((player) => (
              <div className="rounded-lg border border-white/20 p-3" key={`${player.mark}-${player.username}`}>
                <p className="font-semibold text-white">
                  {player.mark} - {player.username}
                  {player.isCurrentPlayer ? " (You)" : ""}
                </p>
                <p className="text-sm text-slate-300">{player.isOnline ? "Online" : "Offline"}</p>
              </div>
            ))}

            <div className="rounded-lg border border-white/20 p-3">
              <p className="text-sm text-slate-300">Share link</p>
              <p className="mt-1 break-all text-xs text-slate-200">{shareLink}</p>
              <Button
                className="mt-3"
                onClick={async () => {
                  await navigator.clipboard.writeText(shareLink);
                }}
                type="button"
                variant="secondary"
              >
                Copy Link
              </Button>
            </div>
          </CardContent>
        </Card>
      </section>
    </main>
  );
}
