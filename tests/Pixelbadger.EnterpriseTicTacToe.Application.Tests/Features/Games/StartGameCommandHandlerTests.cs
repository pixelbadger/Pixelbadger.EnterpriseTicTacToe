using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.StartGame;
using Pixelbadger.EnterpriseTicTacToe.Application.Tests.Support;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Tests.Features.Games;

[TestClass]
public sealed class StartGameCommandHandlerTests
{
    [TestMethod]
    public async Task Handle_CreatesSessionAndPersistsGameState()
    {
        var now = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryGameSessionRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            new QueueSessionCodeGenerator(["ABC123"]),
            now,
            inactivityTimeoutHours: 24);

        var result = await handler.Handle(new StartGameCommand(" Host ", "cookie-1"), CancellationToken.None);

        result.SessionCode.ShouldBe("ABC123");
        result.PlayerCount.ShouldBe(1);
        result.Players.Single().Username.ShouldBe("Host");
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
        repository.Sessions.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task Handle_WhenGeneratedCodeAlreadyExists_RetriesUntilUniqueCode()
    {
        var now = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateWaitingSession(sessionCode: "ABC123", utcNow: now));
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            new QueueSessionCodeGenerator(["ABC123", "XYZ789"]),
            now,
            inactivityTimeoutHours: 24);

        var result = await handler.Handle(new StartGameCommand("Host", "cookie-1"), CancellationToken.None);

        result.SessionCode.ShouldBe("XYZ789");
        repository.Sessions.Count.ShouldBe(2);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task Handle_WhenCodeAllocationExceedsRetries_ThrowsConflict()
    {
        var now = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateWaitingSession(sessionCode: "AAAAAA", utcNow: now));
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            new ConstantSessionCodeGenerator("AAAAAA"),
            now,
            inactivityTimeoutHours: 24);

        await Should.ThrowAsync<ConflictException>(() =>
            handler.Handle(new StartGameCommand("Host", "cookie-1"), CancellationToken.None).AsTask());

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
        repository.Sessions.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task Handle_WhenTimeoutIsNonPositive_UsesMinimumOneHourExpiry()
    {
        var now = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryGameSessionRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(
            repository,
            unitOfWork,
            new QueueSessionCodeGenerator(["ABC123"]),
            now,
            inactivityTimeoutHours: 0);

        await handler.Handle(new StartGameCommand("Host", "cookie-1"), CancellationToken.None);

        repository.Sessions["ABC123"].ExpiresAtUtc.ShouldBe(now.AddHours(1));
    }

    private static StartGameCommandHandler CreateHandler(
        InMemoryGameSessionRepository repository,
        RecordingUnitOfWork unitOfWork,
        QueueSessionCodeGenerator codeGenerator,
        DateTime now,
        int inactivityTimeoutHours)
    {
        return new StartGameCommandHandler(
            repository,
            unitOfWork,
            codeGenerator,
            new PrefixClientIdentityHasher(),
            new AdjustableClock(now),
            Options.Create(new GameSessionSettings { InactivityTimeoutHours = inactivityTimeoutHours }));
    }

    private static StartGameCommandHandler CreateHandler(
        InMemoryGameSessionRepository repository,
        RecordingUnitOfWork unitOfWork,
        ConstantSessionCodeGenerator codeGenerator,
        DateTime now,
        int inactivityTimeoutHours)
    {
        return new StartGameCommandHandler(
            repository,
            unitOfWork,
            codeGenerator,
            new PrefixClientIdentityHasher(),
            new AdjustableClock(now),
            Options.Create(new GameSessionSettings { InactivityTimeoutHours = inactivityTimeoutHours }));
    }
}
