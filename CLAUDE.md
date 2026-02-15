# CLAUDE.md — Project Bootstrap

This file defines the baseline architectural requirements, technology choices, and conventions for this project. All agents and contributors should treat this as authoritative unless a project-specific override exists below.

---

## Backend — C# / .NET

**Runtime & Language:** Latest stable .NET SDK and C# language version. Use latest language constructs (primary constructors, collection expressions, `required` members, file-scoped types, etc.) unless readability suffers.

### Clean Architecture

The solution is organised into the following layers:

- **Domain** — Entities, value objects, and interfaces for infrastructure services. No dependencies on other layers.
- **Application** — Commands, queries, and orchestration logic. Business workflows are expressed as handlers that compose service calls. Services are implemented as *components* with interfaces co-located alongside their implementations.
- **Infrastructure** — Service components that interact with external systems (database, message bus, blob storage, etc.). Implements interfaces defined in Domain.
- **Host / Composition Root** — The runnable application (API, worker, etc.) that composes all layers via dependency injection.

Each layer project exposes a `DependencyInjection.cs` file containing `IServiceCollection` extension methods (e.g. `AddApplicationServices()`, `AddInfrastructureServices()`) that register everything the layer requires. The host calls these at startup.

### Key Libraries

| Concern | Technology |
|---|---|
| Persistence / ORM | Entity Framework Core (latest) |
| Command / Query separation | [Mediator](https://github.com/martinothamar/Mediator) (source-generated, not MediatR) |
| API endpoints | FastEndpoints |
| Validation | FluentValidation |
| Testing framework | MSTest |
| Assertions | Shouldly |

### Conventions

- **Validation pipeline:** Implement a Mediator behaviour (`IPipelineBehavior<TRequest, TResponse>`) that dynamically resolves an `IValidator<TRequest>` from the container and runs it before the handler. If no validator is registered, the behaviour passes through silently.
- **Endpoint structure:** One endpoint class per operation. Endpoints live in the host project (or a dedicated Endpoints project) and dispatch via Mediator.
- **Entity configuration:** EF entity configurations live in Infrastructure alongside the DbContext, one configuration class per entity.
- **Persistence writes / unit of work:** Repositories do not expose `SaveChanges` methods. Mutations are committed through an `IUnitOfWork` abstraction (`SaveChangesAsync`) so each command handler controls a single commit point for all tracked changes.
- **Interface naming:** Prefix all interface names with `I` (for example `IGameSessionRepository`, `IClock`).

### Backend Testing Requirements

- **Framework + assertions:** Backend tests use MSTest + Shouldly.
- **SUT-aligned class naming:** Each SUT has a dedicated `[TestClass]` named `[ClassName]Tests` (for example `GameSessionTests`, `StartGameCommandHandlerTests`). Do not combine unrelated SUTs in one test class.
- **Coverage expectations:** For each SUT, cover happy path plus key rule/guard branches (validation failures, not found/forbidden/conflict paths, and state transitions where applicable).
- **Handler/component behavior:** Tests for command/query handlers verify both returned behavior and orchestration side effects (repository usage, unit-of-work save calls, and exception mapping).
- **Internal components:** Testing internal classes is allowed when needed; expose internals to test assemblies with `InternalsVisibleTo` in a minimal and explicit way.

---

## Frontend — React SPA

**Runtime:** Node.js (LTS). Package manager: **npm**.

This is a client-side single-page application. No server-side rendering.

### Key Libraries

| Concern | Technology |
|---|---|
| UI framework | React (latest, functional components + hooks) |
| Component library | shadcn/ui |
| Forms | react-hook-form |
| State / data fetching | Redux Toolkit + RTK Query |
| Build tool | Vite |
| Testing | Vitest |

### Conventions

- **RTK Query** is the primary mechanism for server communication. Define API slices with endpoints; avoid raw `fetch`/`axios` calls.
- **Forms** use react-hook-form with zod or yup schemas for validation where appropriate.
- **Component structure:** Co-locate component, styles, and tests. Prefer named exports.
- **shadcn/ui** components are copied into the project (not installed as a package). Customise via the project's tailwind theme tokens.

### Frontend Testing Requirements

- **Framework:** Frontend unit/component tests use Vitest.
- **Placement + naming:** Test files should be co-located with their SUT and named `*.test.ts` or `*.test.tsx`.
- **Coverage expectations:** Test render behavior, user interactions, state transitions, and error/edge states for pages, components, hooks, and utility modules touched by the change.
- **Isolation:** Prefer deterministic tests with targeted mocks/stubs for network and realtime dependencies rather than live backend calls.
- **PR gate:** `dotnet test` and frontend Vitest suites must pass in PR validation before merge.

---

## Project Management & CI/CD

### Pipelines (GitHub Actions)

At minimum, every project has:

1. **PR Validation** — Triggers on pull request to main/develop. Builds, lints, and runs all tests. PRs must pass before merge.
2. **CI (Continuous Integration)** — Triggers on push to main/develop. Full build + test + any artifact packaging.
3. **Release** — Case-by-case per project. Define separately when the deployment target is known.

### Dev-Time Orchestration

Use the **.NET Aspire** project (AppHost) for orchestrating services, databases, and other dependencies during local development and build-time integration tests.

---

## General Principles

- Prefer composition over inheritance.
- Prefer explicit over implicit (no magic strings, no convention-over-configuration that hides behaviour).
- Keep third-party abstractions thin — wrap external dependencies behind interfaces defined in Domain or Application.
- Tests are not optional. New behaviour ships with tests.
- When in doubt, ask — don't guess at requirements or invent scope.
