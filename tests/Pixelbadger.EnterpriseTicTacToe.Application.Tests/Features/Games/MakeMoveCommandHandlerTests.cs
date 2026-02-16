using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Application;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.MakeMove;
using Pixelbadger.EnterpriseTicTacToe.Application.Tests.Support;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Tests.Features.Games;

[TestClass]
public sealed class MakeMoveCommandHandlerTests
{
    [TestMethod]
    public async Task Handle_WhenSessionDoesNotExist_ReturnsNotFoundFailure()
    {
        var handler = CreateHandler(new InMemoryGameSessionRepository(), new RecordingUnitOfWork());

        var result = await handler.Handle(new MakeMoveCommand("ABC123", 0, "cookie-1"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.ErrorType.ShouldBe(ResultErrorType.NotFound);
    }

    [TestMethod]
    public async Task Handle_WhenSessionExpired_MarksExpiredAndReturnsNotFoundFailure()
    {
        var now = new DateTime(2026, 2, 12, 12, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryGameSessionRepository();
        var expired = GameSessionFactory.CreateInProgressSession(utcNow: now.AddHours(-5), inactivityTimeoutHours: 1);
        repository.Seed(expired);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork, now);

        var result = await handler.Handle(new MakeMoveCommand("ABC123", 0, "cookie-1"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.ErrorType.ShouldBe(ResultErrorType.NotFound);
        expired.Status.ShouldBe(GameStatus.Expired);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task Handle_WhenDomainRuleFails_ReturnsConflictFailure()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateWaitingSession());
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(new MakeMoveCommand("ABC123", 0, "cookie-1"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.ErrorType.ShouldBe(ResultErrorType.Conflict);
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task Handle_WithValidMove_UpdatesBoardAndSaves()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateInProgressSession());
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(new MakeMoveCommand("ABC123", 0, "cookie-1"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.BoardState.ShouldBe("X........");
        result.Value.CurrentTurn.ShouldBe("O");
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    private static MakeMoveCommandHandler CreateHandler(
        InMemoryGameSessionRepository repository,
        RecordingUnitOfWork unitOfWork,
        DateTime? utcNow = null)
    {
        return new MakeMoveCommandHandler(
            repository,
            unitOfWork,
            new PrefixClientIdentityHasher(),
            new AdjustableClock(utcNow ?? new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc)),
            Options.Create(new GameSessionSettings { InactivityTimeoutHours = 24 }));
    }
}
