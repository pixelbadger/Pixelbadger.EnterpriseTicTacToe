using Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.SetPresence;
using Pixelbadger.EnterpriseTicTacToe.Application.Tests.Support;
using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Tests.Features.Games;

[TestClass]
public sealed class SetPresenceCommandHandlerTests
{
    [TestMethod]
    public async Task Handle_WhenSessionDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateHandler(new InMemoryGameSessionRepository());

        await Should.ThrowAsync<NotFoundException>(() =>
            handler.Handle(new SetPresenceCommand("ABC123", true, "cookie-1"), CancellationToken.None).AsTask());
    }

    [TestMethod]
    public async Task Handle_WhenSessionExpired_ThrowsNotFoundWithoutDurableWrite()
    {
        var now = new DateTime(2026, 2, 12, 12, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryGameSessionRepository();
        var expired = GameSessionFactory.CreateInProgressSession(utcNow: now.AddHours(-5), inactivityTimeoutHours: 1);
        repository.Seed(expired);
        var handler = CreateHandler(repository, now);

        await Should.ThrowAsync<NotFoundException>(() =>
            handler.Handle(new SetPresenceCommand("ABC123", true, "cookie-1"), CancellationToken.None).AsTask());

        expired.Status.ShouldBe(GameStatus.InProgress);
    }

    [TestMethod]
    public async Task Handle_WhenIdentityIsNotPartOfSession_ThrowsForbidden()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateInProgressSession());
        var handler = CreateHandler(repository);

        await Should.ThrowAsync<ForbiddenException>(() =>
            handler.Handle(new SetPresenceCommand("ABC123", true, "cookie-3"), CancellationToken.None).AsTask());
    }

    [TestMethod]
    public async Task Handle_WithValidIdentity_UpdatesPresenceAndSaves()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = GameSessionFactory.CreateInProgressSession();
        repository.Seed(session);
        var presenceTracker = new RecordingGamePresenceTracker();
        presenceTracker.SetPresence("ABC123", "hash::cookie-2", true);
        var handler = CreateHandler(repository, presenceTracker: presenceTracker);

        var result = await handler.Handle(new SetPresenceCommand("ABC123", false, "cookie-2"), CancellationToken.None);

        result.Players.Single(player => player.Mark == "O").IsOnline.ShouldBeFalse();
        presenceTracker.SetPresenceCallCount.ShouldBe(2);
    }

    private static SetPresenceCommandHandler CreateHandler(
        InMemoryGameSessionRepository repository,
        DateTime? utcNow = null,
        RecordingGamePresenceTracker? presenceTracker = null)
    {
        return new SetPresenceCommandHandler(
            repository,
            new RecordingGameSessionCache(),
            new NoOpGameSessionLock(),
            presenceTracker ?? new RecordingGamePresenceTracker(),
            new PrefixClientIdentityHasher(),
            new AdjustableClock(utcNow ?? new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc)));
    }
}
