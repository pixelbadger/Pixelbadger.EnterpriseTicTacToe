import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import { z } from "zod";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../components/ui/card";
import { Input } from "../components/ui/input";
import { Button } from "../components/ui/button";
import { useJoinGameMutation, useStartGameMutation } from "../store/services/gameApi";

const usernameSchema = z
  .string()
  .trim()
  .min(1, "Username is required")
  .max(80, "Username must be 80 characters or fewer");

const startSchema = z.object({
  username: usernameSchema,
});

const joinSchema = z.object({
  sessionCode: z
    .string()
    .trim()
    .regex(/^[A-Za-z0-9]{8}$/, "Session code must be 8 letters/numbers"),
  username: usernameSchema,
});

type StartForm = z.infer<typeof startSchema>;
type JoinForm = z.infer<typeof joinSchema>;

export function LobbyPage() {
  const navigate = useNavigate();
  const params = useParams();
  const [startGame, { isLoading: isStarting }] = useStartGameMutation();
  const [joinGame, { isLoading: isJoining }] = useJoinGameMutation();

  const startForm = useForm<StartForm>({
    resolver: zodResolver(startSchema),
    defaultValues: { username: "" },
  });

  const joinForm = useForm<JoinForm>({
    resolver: zodResolver(joinSchema),
    defaultValues: { sessionCode: params.code ?? "", username: "" },
  });

  const startError = startForm.formState.errors.username?.message;
  const joinCodeError = joinForm.formState.errors.sessionCode?.message;
  const joinUsernameError = joinForm.formState.errors.username?.message;

  return (
    <main className="min-h-screen bg-grid py-8">
      <section className="mx-auto grid w-full max-w-6xl gap-6 px-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Start a New Session</CardTitle>
            <CardDescription>
              Create a private game with an 8-character code and share the link with your opponent.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form
              className="space-y-4"
              onSubmit={startForm.handleSubmit(async (values) => {
                const gameState = await startGame({ username: values.username }).unwrap();
                navigate(`/game/${gameState.sessionCode}`, { state: { gameState } });
              })}
            >
              <label className="block text-sm font-medium text-slate-200" htmlFor="start-username">
                Username
              </label>
              <Input id="start-username" maxLength={80} {...startForm.register("username")} />
              {startError ? <p className="text-sm text-rose-300">{startError}</p> : null}

              <Button className="w-full" disabled={isStarting} type="submit">
                {isStarting ? "Starting..." : "Start Game"}
              </Button>
            </form>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Join an Existing Session</CardTitle>
            <CardDescription>
              Use a shared code or link. Manual rejoin requires your original browser cookie identity.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form
              className="space-y-4"
              onSubmit={joinForm.handleSubmit(async (values) => {
                const gameState = await joinGame({
                  sessionCode: values.sessionCode.toUpperCase(),
                  username: values.username,
                }).unwrap();
                navigate(`/game/${gameState.sessionCode}`, { state: { gameState } });
              })}
            >
              <label className="block text-sm font-medium text-slate-200" htmlFor="join-code">
                Session Code
              </label>
              <Input id="join-code" maxLength={8} {...joinForm.register("sessionCode")} />
              {joinCodeError ? <p className="text-sm text-rose-300">{joinCodeError}</p> : null}

              <label className="block text-sm font-medium text-slate-200" htmlFor="join-username">
                Username
              </label>
              <Input id="join-username" maxLength={80} {...joinForm.register("username")} />
              {joinUsernameError ? <p className="text-sm text-rose-300">{joinUsernameError}</p> : null}

              <Button className="w-full" disabled={isJoining} type="submit" variant="secondary">
                {isJoining ? "Joining..." : "Join Game"}
              </Button>
            </form>
          </CardContent>
        </Card>
      </section>
    </main>
  );
}
