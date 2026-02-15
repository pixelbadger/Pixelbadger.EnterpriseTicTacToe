using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.SetPresence;
using Pixelbadger.EnterpriseTicTacToe.Application.Tests.Support;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Tests.Features.Games;

[TestClass]
public sealed class SetPresenceCommandHandlerTests
{
    [TestMethod]
    public async Task Handle_WhenSessionDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateHandler(new InMemoryGameSessionRepository(), new RecordingUnitOfWork());

        await Should.ThrowAsync<NotFoundException>(() =>
            handler.Handle(new SetPresenceCommand("ABC123", true, "cookie-1"), CancellationToken.None).AsTask());
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
            handler.Handle(new SetPresenceCommand("ABC123", true, "cookie-1"), CancellationToken.None).AsTask());

        expired.Status.ShouldBe(GameStatus.Expired);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task Handle_WhenIdentityIsNotPartOfSession_ThrowsForbidden()
    {
        var repository = new InMemoryGameSessionRepository();
        repository.Seed(GameSessionFactory.CreateInProgressSession());
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        await Should.ThrowAsync<ForbiddenException>(() =>
            handler.Handle(new SetPresenceCommand("ABC123", true, "cookie-3"), CancellationToken.None).AsTask());

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task Handle_WithValidIdentity_UpdatesPresenceAndSaves()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = GameSessionFactory.CreateInProgressSession();
        repository.Seed(session);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(new SetPresenceCommand("ABC123", false, "cookie-2"), CancellationToken.None);

        result.Players.Single(player => player.Mark == "O").IsOnline.ShouldBeFalse();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    private static SetPresenceCommandHandler CreateHandler(
        InMemoryGameSessionRepository repository,
        RecordingUnitOfWork unitOfWork,
        DateTime? utcNow = null)
    {
        return new SetPresenceCommandHandler(
            repository,
            unitOfWork,
            new PrefixClientIdentityHasher(),
            new AdjustableClock(utcNow ?? new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc)),
            Options.Create(new GameSessionSettings { InactivityTimeoutHours = 24 }));
    }
}
