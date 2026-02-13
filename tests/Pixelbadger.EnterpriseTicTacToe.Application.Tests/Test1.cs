using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.JoinGame;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.StartGame;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Tests;

[TestClass]
public sealed class GameCommandTests
{
    [TestMethod]
    public async Task StartGame_CreatesSessionWithNormalizedIdentity()
    {
        var repository = new InMemoryRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var handler = new StartGameCommandHandler(
            repository,
            unitOfWork,
            new StubCodeGenerator("ABC123"),
            new StubHasher(),
            new StubClock(),
            Options.Create(new GameSessionSettings { InactivityTimeoutHours = 24 }));

        var result = await handler.Handle(new StartGameCommand("Host", "cookie-1"), CancellationToken.None);

        result.SessionCode.ShouldBe("ABC123");
        result.PlayerCount.ShouldBe(1);
        result.Players.Single().Username.ShouldBe("Host");
        repository.Sessions.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task JoinGame_WithDifferentIdentityOnExistingUsername_ThrowsForbidden()
    {
        var repository = new InMemoryRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var startHandler = new StartGameCommandHandler(
            repository,
            unitOfWork,
            new StubCodeGenerator("ABC123"),
            new StubHasher(),
            new StubClock(),
            Options.Create(new GameSessionSettings { InactivityTimeoutHours = 24 }));
        var joinHandler = new JoinGameCommandHandler(
            repository,
            unitOfWork,
            new StubHasher(),
            new StubClock(),
            Options.Create(new GameSessionSettings { InactivityTimeoutHours = 24 }));

        await startHandler.Handle(new StartGameCommand("Host", "cookie-1"), CancellationToken.None);

        await Should.ThrowAsync<ForbiddenException>(() =>
            joinHandler.Handle(new JoinGameCommand("ABC123", "Host", "cookie-2"), CancellationToken.None).AsTask());
    }

    [TestMethod]
    public async Task JoinGame_WithAvailableSeat_AddsOpponent()
    {
        var repository = new InMemoryRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var startHandler = new StartGameCommandHandler(
            repository,
            unitOfWork,
            new StubCodeGenerator("ABC123"),
            new StubHasher(),
            new StubClock(),
            Options.Create(new GameSessionSettings { InactivityTimeoutHours = 24 }));
        var joinHandler = new JoinGameCommandHandler(
            repository,
            unitOfWork,
            new StubHasher(),
            new StubClock(),
            Options.Create(new GameSessionSettings { InactivityTimeoutHours = 24 }));

        await startHandler.Handle(new StartGameCommand("Host", "cookie-1"), CancellationToken.None);
        var joined = await joinHandler.Handle(new JoinGameCommand("ABC123", "Guest", "cookie-2"), CancellationToken.None);

        joined.PlayerCount.ShouldBe(2);
        joined.Status.ShouldBe("InProgress");
        joined.Players.Any(player => player.Username == "Guest" && player.Mark == "O").ShouldBeTrue();
    }

    [TestMethod]
    public async Task JoinGame_WithSameIdentityAndDifferentUsername_ThrowsForbidden()
    {
        var repository = new InMemoryRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var startHandler = new StartGameCommandHandler(
            repository,
            unitOfWork,
            new StubCodeGenerator("ABC123"),
            new StubHasher(),
            new StubClock(),
            Options.Create(new GameSessionSettings { InactivityTimeoutHours = 24 }));
        var joinHandler = new JoinGameCommandHandler(
            repository,
            unitOfWork,
            new StubHasher(),
            new StubClock(),
            Options.Create(new GameSessionSettings { InactivityTimeoutHours = 24 }));

        await startHandler.Handle(new StartGameCommand("Host", "cookie-1"), CancellationToken.None);

        await Should.ThrowAsync<ForbiddenException>(() =>
            joinHandler.Handle(new JoinGameCommand("ABC123", "OtherName", "cookie-1"), CancellationToken.None).AsTask());
    }

    private sealed class InMemoryRepository : IGameSessionRepository
    {
        public Dictionary<string, GameSession> Sessions { get; } = [];

        public Task<GameSession?> GetByCode(string sessionCode, CancellationToken cancellationToken)
        {
            Sessions.TryGetValue(sessionCode, out var session);
            return Task.FromResult(session);
        }

        public Task Add(GameSession session, CancellationToken cancellationToken)
        {
            Sessions[session.SessionCode] = session;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubCodeGenerator(string code) : ISessionCodeGenerator
    {
        public string GenerateCode() => code;
    }

    private sealed class StubHasher : IClientIdentityHasher
    {
        public string Hash(string clientIdentity) => $"hash::{clientIdentity}";
    }

    private sealed class StubClock : IClock
    {
        public DateTime UtcNow => new(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);
    }
}
