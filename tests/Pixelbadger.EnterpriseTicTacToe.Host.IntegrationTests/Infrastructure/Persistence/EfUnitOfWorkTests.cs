using Microsoft.EntityFrameworkCore;
using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Data;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Persistence;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Host.IntegrationTests.Infrastructure.Persistence;

[TestClass]
public sealed class EfUnitOfWorkTests
{
    [TestMethod]
    public async Task SaveChangesAsync_PersistsPendingChanges()
    {
        await using var context = new TicTacToeDbContext(new DbContextOptionsBuilder<TicTacToeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        var unitOfWork = new EfUnitOfWork(context);
        var now = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);

        context.GameSessions.Add(GameSession.Create(
            sessionCode: "ABC123",
            username: "Host",
            normalizedUsername: "HOST",
            clientIdentityHash: "hash-x",
            utcNow: now,
            expiresAtUtc: now.AddHours(24)));

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        context.GameSessions.Count().ShouldBe(1);
    }
}
