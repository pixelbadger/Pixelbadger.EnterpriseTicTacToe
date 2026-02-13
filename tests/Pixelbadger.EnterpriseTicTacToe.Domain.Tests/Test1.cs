using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;
using Pixelbadger.EnterpriseTicTacToe.Domain.Exceptions;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Domain.Tests;

[TestClass]
public sealed class GameSessionTests
{
    [TestMethod]
    public void JoinOpponent_AssignsSecondSeatAndStartsGame()
    {
        var now = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var session = CreateSession(now);

        session.JoinOpponent("Guest", "GUEST", "hash-o", now, now.AddHours(24));

        session.PlayerCount.ShouldBe(2);
        session.Status.ShouldBe(GameStatus.InProgress);
        session.CurrentTurn.ShouldBe(PlayerMark.X);
        session.Players.Any(player => player.Mark == PlayerMark.O && player.Username == "Guest").ShouldBeTrue();
    }

    [TestMethod]
    public void MakeMove_WithWinningLine_SetsWinnerAndStopsTurns()
    {
        var now = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var session = CreateSession(now);
        session.JoinOpponent("Guest", "GUEST", "hash-o", now, now.AddHours(24));

        session.MakeMove("hash-x", 0, now, now.AddHours(24));
        session.MakeMove("hash-o", 3, now, now.AddHours(24));
        session.MakeMove("hash-x", 1, now, now.AddHours(24));
        session.MakeMove("hash-o", 4, now, now.AddHours(24));
        session.MakeMove("hash-x", 2, now, now.AddHours(24));

        session.Status.ShouldBe(GameStatus.Won);
        session.Winner.ShouldBe(PlayerMark.X);
        session.CurrentTurn.ShouldBeNull();
        session.BoardState.ShouldBe("XXXOO....");
    }

    [TestMethod]
    public void RegisterRematchVote_RequiresBothPlayers()
    {
        var now = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var session = CreateSession(now);
        session.JoinOpponent("Guest", "GUEST", "hash-o", now, now.AddHours(24));

        session.MakeMove("hash-x", 0, now, now.AddHours(24));
        session.MakeMove("hash-o", 3, now, now.AddHours(24));
        session.MakeMove("hash-x", 1, now, now.AddHours(24));
        session.MakeMove("hash-o", 4, now, now.AddHours(24));
        session.MakeMove("hash-x", 2, now, now.AddHours(24));

        var firstVote = session.RegisterRematchVote("hash-x", now, now.AddHours(24));
        var secondVote = session.RegisterRematchVote("hash-o", now, now.AddHours(24));

        firstVote.ShouldBeFalse();
        secondVote.ShouldBeTrue();
        session.Status.ShouldBe(GameStatus.InProgress);
        session.CurrentTurn.ShouldBe(PlayerMark.X);
        session.Winner.ShouldBeNull();
        session.BoardState.ShouldBe(".........");
    }

    [TestMethod]
    public void MakeMove_OutOfTurn_Throws()
    {
        var now = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var session = CreateSession(now);
        session.JoinOpponent("Guest", "GUEST", "hash-o", now, now.AddHours(24));

        Should.Throw<DomainRuleViolationException>(() =>
            session.MakeMove("hash-o", 0, now, now.AddHours(24)));
    }

    private static GameSession CreateSession(DateTime now)
    {
        return GameSession.Create(
            sessionCode: "ABC123",
            username: "Host",
            normalizedUsername: "HOST",
            clientIdentityHash: "hash-x",
            utcNow: now,
            expiresAtUtc: now.AddHours(24));
    }
}
