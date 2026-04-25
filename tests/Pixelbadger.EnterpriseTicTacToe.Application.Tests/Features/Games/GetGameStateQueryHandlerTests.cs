using Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.GetGameState;
using Pixelbadger.EnterpriseTicTacToe.Application.Tests.Support;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Tests.Features.Games;

[TestClass]
public sealed class GetGameStateQueryHandlerTests
{
    [TestMethod]
    public async Task Handle_WhenSessionDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateHandler(new InMemoryGameSessionRepository());

        await Should.ThrowAsync<NotFoundException>(() =>
            handler.Handle(new GetGameStateQuery("ABC123", "cookie-1"), CancellationToken.None).AsTask());
    }

    [TestMethod]
    public async Task Handle_WhenSessionExpired_ThrowsNotFound()
    {
        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateInProgressSession(utcNow: now.AddHours(-3), inactivityTimeoutHours: 1));
        var handler = CreateHandler(repository, now);

        await Should.ThrowAsync<NotFoundException>(() =>
            handler.Handle(new GetGameStateQuery("ABC123", "cookie-1"), CancellationToken.None).AsTask());
    }

    [TestMethod]
    public async Task Handle_WhenIdentityIsNotPartOfGame_ThrowsForbidden()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateInProgressSession());
        var handler = CreateHandler(repository);

        await Should.ThrowAsync<ForbiddenException>(() =>
            handler.Handle(new GetGameStateQuery("ABC123", "cookie-3"), CancellationToken.None).AsTask());
    }

    [TestMethod]
    public async Task Handle_WithValidIdentity_ReturnsCurrentState()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateInProgressSession());
        var handler = CreateHandler(repository);

        var state = await handler.Handle(new GetGameStateQuery("ABC123", "cookie-1"), CancellationToken.None);

        state.SessionCode.ShouldBe("ABC123");
        state.PlayerCount.ShouldBe(2);
        state.Players.Single(player => player.Mark == "X").IsCurrentPlayer.ShouldBeTrue();
    }

    private static GetGameStateQueryHandler CreateHandler(InMemoryGameSessionRepository repository, DateTime? utcNow = null)
    {
        return new GetGameStateQueryHandler(
            repository,
            new RecordingGameSessionCache(),
            new NoOpGameSessionLock(),
            new RecordingGamePresenceTracker(),
            new PrefixClientIdentityHasher(),
            new AdjustableClock(utcNow ?? new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc)));
    }
}
