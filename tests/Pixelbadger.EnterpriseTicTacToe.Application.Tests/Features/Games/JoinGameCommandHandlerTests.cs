using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Application;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.JoinGame;
using Pixelbadger.EnterpriseTicTacToe.Application.Tests.Support;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Tests.Features.Games;

[TestClass]
public sealed class JoinGameCommandHandlerTests
{
    [TestMethod]
    public async Task Handle_WhenSessionDoesNotExist_ReturnsNotFoundFailure()
    {
        var repository = new InMemoryGameSessionRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(new JoinGameCommand("ABC123", "Guest", "cookie-2"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.ErrorType.ShouldBe(ResultErrorType.NotFound);
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task Handle_WhenSessionExpired_MarksExpiredAndReturnsNotFoundFailure()
    {
        var now = new DateTime(2026, 2, 12, 12, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryGameSessionRepository();
        var expired = GameSessionFactory.CreateWaitingSession(utcNow: now.AddHours(-5), inactivityTimeoutHours: 1);
        repository.Seed(expired);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork, now);

        var result = await handler.Handle(new JoinGameCommand("ABC123", "Guest", "cookie-2"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.ErrorType.ShouldBe(ResultErrorType.NotFound);
        expired.Status.ShouldBe(GameStatus.Expired);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task Handle_WhenIdentityAlreadyInSessionWithDifferentUsername_ReturnsForbiddenFailure()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateWaitingSession());
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(new JoinGameCommand("ABC123", "OtherName", "cookie-1"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.ErrorType.ShouldBe(ResultErrorType.Forbidden);
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task Handle_WhenUsernameBoundToDifferentIdentity_ReturnsForbiddenFailure()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateWaitingSession());
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(new JoinGameCommand("ABC123", "Host", "cookie-2"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.ErrorType.ShouldBe(ResultErrorType.Forbidden);
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task Handle_WhenIdentityRejoinsKnownSeat_UpdatesPresenceAndReturnsState()
    {
        var now = new DateTime(2026, 2, 12, 0, 2, 0, DateTimeKind.Utc);
        var repository = new InMemoryGameSessionRepository();
        var session = GameSessionFactory.CreateWaitingSession();
        session.SetPresence("hash::cookie-1", false, now.AddMinutes(-1), now.AddHours(24));
        repository.Seed(session);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork, now);

        var result = await handler.Handle(new JoinGameCommand(" abc123 ", "Host", "cookie-1"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.PlayerCount.ShouldBe(1);
        result.Value.Players.Single().IsOnline.ShouldBeTrue();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task Handle_WhenSeatAvailable_JoinsOpponentAndStartsGame()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateWaitingSession());
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(new JoinGameCommand("ABC123", "Guest", "cookie-2"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.PlayerCount.ShouldBe(2);
        result.Value.Status.ShouldBe("InProgress");
        result.Value.Players.Any(player => player.Username == "Guest" && player.Mark == "O").ShouldBeTrue();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task Handle_WhenGameAlreadyFull_ReturnsConflictFailure()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateInProgressSession());
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(new JoinGameCommand("ABC123", "Third", "cookie-3"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.ErrorType.ShouldBe(ResultErrorType.Conflict);
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    private static JoinGameCommandHandler CreateHandler(
        InMemoryGameSessionRepository repository,
        RecordingUnitOfWork unitOfWork,
        DateTime? utcNow = null)
    {
        return new JoinGameCommandHandler(
            repository,
            unitOfWork,
            new PrefixClientIdentityHasher(),
            new AdjustableClock(utcNow ?? new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc)),
            Options.Create(new GameSessionSettings { InactivityTimeoutHours = 24 }));
    }
}
