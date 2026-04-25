using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.RequestRematch;
using Pixelbadger.EnterpriseTicTacToe.Application.Tests.Support;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Tests.Features.Games;

[TestClass]
public sealed class RequestRematchCommandHandlerTests
{
    [TestMethod]
    public async Task Handle_WhenSessionDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateHandler(new InMemoryGameSessionRepository(), new RecordingUnitOfWork());

        await Should.ThrowAsync<NotFoundException>(() =>
            handler.Handle(new RequestRematchCommand("ABC123", "cookie-1"), CancellationToken.None).AsTask());
    }

    [TestMethod]
    public async Task Handle_WhenSessionExpired_MarksExpiredAndThrowsNotFound()
    {
        var now = new DateTime(2026, 2, 12, 12, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryGameSessionRepository();
        var expired = GameSessionFactory.CreateInProgressSession(utcNow: now.AddHours(-5), inactivityTimeoutHours: 1);
        repository.Seed(expired);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork, now);

        await Should.ThrowAsync<NotFoundException>(() =>
            handler.Handle(new RequestRematchCommand("ABC123", "cookie-1"), CancellationToken.None).AsTask());

        expired.Status.ShouldBe(GameStatus.Expired);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task Handle_WhenDomainRuleFails_ThrowsConflict()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateInProgressSession());
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        await Should.ThrowAsync<ConflictException>(() =>
            handler.Handle(new RequestRematchCommand("ABC123", "cookie-1"), CancellationToken.None).AsTask());

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task Handle_WithCompletedGame_RegistersVoteAndSaves()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateCompletedSession());
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(new RequestRematchCommand("ABC123", "cookie-1"), CancellationToken.None);

        result.RematchXReady.ShouldBeTrue();
        result.RematchOReady.ShouldBeFalse();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    private static RequestRematchCommandHandler CreateHandler(
        InMemoryGameSessionRepository repository,
        RecordingUnitOfWork unitOfWork,
        DateTime? utcNow = null)
    {
        return new RequestRematchCommandHandler(
            repository,
            unitOfWork,
            new RecordingGameSessionCache(),
            new NoOpGameSessionLock(),
            new RecordingGamePresenceTracker(),
            new PrefixClientIdentityHasher(),
            new AdjustableClock(utcNow ?? new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc)),
            Options.Create(new GameSessionSettings { InactivityTimeoutHours = 24 }));
    }
}
