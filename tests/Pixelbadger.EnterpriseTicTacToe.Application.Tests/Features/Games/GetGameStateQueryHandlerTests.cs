using Pixelbadger.EnterpriseTicTacToe.Application;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.GetGameState;
using Pixelbadger.EnterpriseTicTacToe.Application.Tests.Support;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Tests.Features.Games;

[TestClass]
public sealed class GetGameStateQueryHandlerTests
{
    [TestMethod]
    public async Task Handle_WhenSessionDoesNotExist_ReturnsNotFoundFailure()
    {
        var handler = CreateHandler(new InMemoryGameSessionRepository());

        var result = await handler.Handle(new GetGameStateQuery("ABC123", "cookie-1"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.ErrorType.ShouldBe(ResultErrorType.NotFound);
    }

    [TestMethod]
    public async Task Handle_WhenSessionExpired_ReturnsNotFoundFailure()
    {
        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateInProgressSession(utcNow: now.AddHours(-3), inactivityTimeoutHours: 1));
        var handler = CreateHandler(repository, now);

        var result = await handler.Handle(new GetGameStateQuery("ABC123", "cookie-1"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.ErrorType.ShouldBe(ResultErrorType.NotFound);
    }

    [TestMethod]
    public async Task Handle_WhenIdentityIsNotPartOfGame_ReturnsForbiddenFailure()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateInProgressSession());
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new GetGameStateQuery("ABC123", "cookie-3"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.ErrorType.ShouldBe(ResultErrorType.Forbidden);
    }

    [TestMethod]
    public async Task Handle_WithValidIdentity_ReturnsCurrentState()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateInProgressSession());
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new GetGameStateQuery("ABC123", "cookie-1"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.SessionCode.ShouldBe("ABC123");
        result.Value.PlayerCount.ShouldBe(2);
        result.Value.Players.Single(player => player.Mark == "X").IsCurrentPlayer.ShouldBeTrue();
    }

    private static GetGameStateQueryHandler CreateHandler(InMemoryGameSessionRepository repository, DateTime? utcNow = null)
    {
        return new GetGameStateQueryHandler(
            repository,
            new PrefixClientIdentityHasher(),
            new AdjustableClock(utcNow ?? new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc)));
    }
}
