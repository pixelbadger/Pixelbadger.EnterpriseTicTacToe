using Microsoft.EntityFrameworkCore;
using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Data;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Persistence;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Host.IntegrationTests.Infrastructure.Persistence;

[TestClass]
public sealed class EfGameSessionRepositoryTests
{
    [TestMethod]
    public async Task GetByCode_WhenSessionExists_ReturnsSessionWithPlayers()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var now = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);

        await using (var seedContext = CreateDbContext(dbName))
        {
            var session = CreateSession(now);
            seedContext.GameSessions.Add(session);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(dbName);
        var repository = new EfGameSessionRepository(readContext);

        var loaded = await repository.GetByCode("ABC123", CancellationToken.None);

        loaded.ShouldNotBeNull();
        loaded.PlayerCount.ShouldBe(2);
        loaded.Players.Select(player => player.Mark.ToString()).ShouldBe(["X", "O"]);
    }

    [TestMethod]
    public async Task GetByCode_WhenSessionDoesNotExist_ReturnsNull()
    {
        await using var context = CreateDbContext(Guid.NewGuid().ToString("N"));
        var repository = new EfGameSessionRepository(context);

        var loaded = await repository.GetByCode("ABC123", CancellationToken.None);

        loaded.ShouldBeNull();
    }

    [TestMethod]
    public async Task Add_AddsSessionToDbContext()
    {
        await using var context = CreateDbContext(Guid.NewGuid().ToString("N"));
        var repository = new EfGameSessionRepository(context);
        var session = CreateSession(new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc));

        await repository.Add(session, CancellationToken.None);
        await context.SaveChangesAsync();

        context.GameSessions.Count().ShouldBe(1);
    }

    private static TicTacToeDbContext CreateDbContext(string dbName)
    {
        return new TicTacToeDbContext(new DbContextOptionsBuilder<TicTacToeDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);
    }

    private static GameSession CreateSession(DateTime now)
    {
        var session = GameSession.Create(
            sessionCode: "ABC123",
            username: "Host",
            normalizedUsername: "HOST",
            clientIdentityHash: "hash-x",
            utcNow: now,
            expiresAtUtc: now.AddHours(24));

        session.JoinOpponent("Guest", "GUEST", "hash-o", now, now.AddHours(24));
        return session;
    }
}
