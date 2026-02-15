# Enterprise Tic-Tac-Toe

Anonymous, cookie-bound, multiplayer tic-tac-toe with a React SPA frontend and .NET backend.

## Architecture

- `src/Pixelbadger.EnterpriseTicTacToe.Domain` - core entities, rules, and infrastructure interfaces.
- `src/Pixelbadger.EnterpriseTicTacToe.Application` - command/query handlers, validation, and DTO contracts.
- `src/Pixelbadger.EnterpriseTicTacToe.Infrastructure` - EF Core SQL Server persistence and service implementations.
- `src/Pixelbadger.EnterpriseTicTacToe.Host` - FastEndpoints API, SignalR hub, cookie identity middleware.
- `src/Pixelbadger.EnterpriseTicTacToe.AppHost` - .NET Aspire orchestration.
- `src/Pixelbadger.EnterpriseTicTacToe.Database` - DbUp migration runner and SQL schema scripts used for deployment.
- `frontend` - React/Vite SPA with RTK Query and SignalR client.

## Feature Highlights

- Start or join game sessions with 6-character codes.
- `/join/{code}` share links prefill join flow.
- Maximum 2 players per session (X and O).
- Username required (max 80 chars), unique within session.
- Backend authoritative game state (board, turns, status, rematch votes, presence).
- Cookie-bound anonymous identity with HMAC hashing server-side.
- Manual rejoin only; same cookie identity required for seat ownership.
- SignalR real-time updates for board, turn, presence, and rematch.
- Rematch requires both players to vote.

## Local Run

### Backend

1. Set SQL Server connection string in `src/Pixelbadger.EnterpriseTicTacToe.Host/appsettings.Development.json`.
2. Set `ClientIdentity:HashKey` from a secure local secret source.
3. Apply database schema scripts:

```bash
dotnet run --project src/Pixelbadger.EnterpriseTicTacToe.Database -- --connection-string "<your-connection-string>"
```

4. Run:

```bash
dotnet run --project src/Pixelbadger.EnterpriseTicTacToe.Host
```

### Frontend

1. Install Node.js LTS and npm.
2. Run:

```bash
cd frontend
npm install
npm run dev
```

Vite proxies `/api` and `/hubs` to Aspire-provided API endpoint environment variables when available, otherwise to `http://localhost:5217`.

### Full Aspire (API + SQL + Vite)

```bash
dotnet run --project src/Pixelbadger.EnterpriseTicTacToe.AppHost
```

The AppHost orchestrates SQL Server and overrides `ConnectionStrings:DefaultConnection` for both the API and the DbUp runner to use the Aspire SQL instance.
Use the `dbup` resource from the Aspire dashboard when you want to run schema scripts against the orchestrated database.
Aspire defaults to Docker; set `ASPIRE_CONTAINER_RUNTIME=podman` in your local environment if you want to run containers with Podman.

### Frontend Production Build Output

Frontend production assets are emitted to `src/Pixelbadger.EnterpriseTicTacToe.Host/wwwroot` so the Host project can serve the SPA directly.

## Tests

```bash
dotnet test Pixelbadger.EnterpriseTicTacToe.slnx
```

Frontend tests:

```bash
cd frontend
npm test
```

## CI Deploy Prerequisites

- GitHub OIDC deploy principal needs Azure RBAC at deploy scope that can create role assignments (for example `Contributor` + `User Access Administrator` on the target resource group).
- DbUp in CI uses Azure AD auth from `ConnectionStrings__DefaultConnection`; the OIDC principal must exist as a SQL user with migration permissions.
- Use `scripts/sql/bootstrap-github-oidc-dbup.sql` to create the SQL user and grant migration roles (`db_ddladmin`, `db_datareader`, `db_datawriter`) for the OIDC principal.
- Repository variable `AZURE_API_WEBAPP_NAME` is optional, but recommended to avoid web app discovery ambiguity during DbUp.
