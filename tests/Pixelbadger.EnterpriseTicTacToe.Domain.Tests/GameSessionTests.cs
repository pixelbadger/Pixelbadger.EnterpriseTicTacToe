using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;
using Pixelbadger.EnterpriseTicTacToe.Domain.Exceptions;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Domain.Tests;

[TestClass]
public sealed class GameSessionTests
{
    [TestMethod]
    public void Create_InitializesExpectedDefaults()
    {
        var now = Utc(0);

        var session = CreateSession(now);

        session.SessionCode.ShouldBe("ABC123");
        session.Status.ShouldBe(GameStatus.WaitingForOpponent);
        session.CurrentTurn.ShouldBe(PlayerMark.X);
        session.BoardState.ShouldBe(".........");
        session.PlayerCount.ShouldBe(1);
        session.Players.Single().Mark.ShouldBe(PlayerMark.X);
        session.LastActivityUtc.ShouldBe(now);
    }

    [TestMethod]
    public void JoinOpponent_AssignsSecondSeatAndStartsGame()
    {
        var now = Utc(0);
        var session = CreateSession(now);

        session.JoinOpponent("Guest", "GUEST", "hash-o", now, now.AddHours(24));

        session.PlayerCount.ShouldBe(2);
        session.Status.ShouldBe(GameStatus.InProgress);
        session.CurrentTurn.ShouldBe(PlayerMark.X);
        session.Players.Any(player => player.Mark == PlayerMark.O && player.Username == "Guest").ShouldBeTrue();
    }

    [TestMethod]
    public void JoinOpponent_WhenGameIsFull_Throws()
    {
        var now = Utc(0);
        var session = CreateInProgressSession(now);

        Should.Throw<DomainRuleViolationException>(() =>
            session.JoinOpponent("Third", "THIRD", "hash-third", now, now.AddHours(24)));
    }

    [TestMethod]
    public void JoinOpponent_WithDuplicateUsername_Throws()
    {
        var now = Utc(0);
        var session = CreateSession(now);

        Should.Throw<DomainRuleViolationException>(() =>
            session.JoinOpponent("Host", "HOST", "hash-o", now, now.AddHours(24)));
    }

    [TestMethod]
    public void SetPresence_UpdatesPlayerAndActivity()
    {
        var now = Utc(0);
        var session = CreateInProgressSession(now);
        var updateTime = now.AddMinutes(2);

        session.SetPresence("hash-o", false, updateTime, updateTime.AddHours(12));

        var player = session.FindPlayerByIdentity("hash-o");
        player.ShouldNotBeNull();
        player.IsOnline.ShouldBeFalse();
        player.LastSeenUtc.ShouldBe(updateTime);
        session.LastActivityUtc.ShouldBe(updateTime);
    }

    [TestMethod]
    public void SetPresence_ForUnknownIdentity_Throws()
    {
        var now = Utc(0);
        var session = CreateInProgressSession(now);

        Should.Throw<DomainRuleViolationException>(() =>
            session.SetPresence("hash-unknown", true, now, now.AddHours(24)));
    }

    [TestMethod]
    public void MakeMove_WithWinningLine_SetsWinnerAndStopsTurns()
    {
        var now = Utc(0);
        var session = CreateInProgressSession(now);

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
    public void MakeMove_WithDraw_SetsDrawState()
    {
        var now = Utc(0);
        var session = CreateInProgressSession(now);

        session.MakeMove("hash-x", 0, now, now.AddHours(24));
        session.MakeMove("hash-o", 1, now, now.AddHours(24));
        session.MakeMove("hash-x", 2, now, now.AddHours(24));
        session.MakeMove("hash-o", 4, now, now.AddHours(24));
        session.MakeMove("hash-x", 3, now, now.AddHours(24));
        session.MakeMove("hash-o", 5, now, now.AddHours(24));
        session.MakeMove("hash-x", 7, now, now.AddHours(24));
        session.MakeMove("hash-o", 6, now, now.AddHours(24));
        session.MakeMove("hash-x", 8, now, now.AddHours(24));

        session.BoardState.ShouldBe("XOXXOOOXX");
        session.Status.ShouldBe(GameStatus.Draw);
        session.Winner.ShouldBeNull();
        session.CurrentTurn.ShouldBeNull();
    }

    [TestMethod]
    public void MakeMove_OutOfTurn_Throws()
    {
        var now = Utc(0);
        var session = CreateInProgressSession(now);

        Should.Throw<DomainRuleViolationException>(() =>
            session.MakeMove("hash-o", 0, now, now.AddHours(24)));
    }

    [TestMethod]
    public void MakeMove_BeforeGameStarts_Throws()
    {
        var now = Utc(0);
        var session = CreateSession(now);

        Should.Throw<DomainRuleViolationException>(() =>
            session.MakeMove("hash-x", 0, now, now.AddHours(24)));
    }

    [TestMethod]
    public void MakeMove_WhenCellOccupied_Throws()
    {
        var now = Utc(0);
        var session = CreateInProgressSession(now);
        session.MakeMove("hash-x", 0, now, now.AddHours(24));

        Should.Throw<DomainRuleViolationException>(() =>
            session.MakeMove("hash-o", 0, now, now.AddHours(24)));
    }

    [TestMethod]
    public void MakeMove_ForUnknownPlayer_Throws()
    {
        var now = Utc(0);
        var session = CreateInProgressSession(now);

        Should.Throw<DomainRuleViolationException>(() =>
            session.MakeMove("hash-unknown", 0, now, now.AddHours(24)));
    }

    [TestMethod]
    public void RegisterRematchVote_RequiresBothPlayers()
    {
        var now = Utc(0);
        var session = CreateCompletedSession(now);

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
    public void RegisterRematchVote_WhenGameNotCompleted_Throws()
    {
        var now = Utc(0);
        var session = CreateInProgressSession(now);

        Should.Throw<DomainRuleViolationException>(() =>
            session.RegisterRematchVote("hash-x", now, now.AddHours(24)));
    }

    [TestMethod]
    public void RegisterRematchVote_ForUnknownPlayer_Throws()
    {
        var now = Utc(0);
        var session = CreateCompletedSession(now);

        Should.Throw<DomainRuleViolationException>(() =>
            session.RegisterRematchVote("hash-unknown", now, now.AddHours(24)));
    }

    [TestMethod]
    public void MarkExpired_SetsExpiredState()
    {
        var now = Utc(0);
        var session = CreateCompletedSession(now);
        var expireAt = now.AddMinutes(10);

        session.MarkExpired(expireAt);

        session.Status.ShouldBe(GameStatus.Expired);
        session.CurrentTurn.ShouldBeNull();
        session.Winner.ShouldBeNull();
        session.UpdatedUtc.ShouldBe(expireAt);
        session.LastActivityUtc.ShouldBe(expireAt);
    }

    [TestMethod]
    public void IsExpired_ReturnsTrueWhenStatusOrTimestampExpired()
    {
        var now = Utc(0);
        var session = CreateSession(now);

        session.IsExpired(now.AddHours(25)).ShouldBeTrue();
        session.MarkExpired(now.AddHours(1));
        session.IsExpired(now).ShouldBeTrue();
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

    private static GameSession CreateInProgressSession(DateTime now)
    {
        var session = CreateSession(now);
        session.JoinOpponent("Guest", "GUEST", "hash-o", now, now.AddHours(24));
        return session;
    }

    private static GameSession CreateCompletedSession(DateTime now)
    {
        var session = CreateInProgressSession(now);
        session.MakeMove("hash-x", 0, now, now.AddHours(24));
        session.MakeMove("hash-o", 3, now, now.AddHours(24));
        session.MakeMove("hash-x", 1, now, now.AddHours(24));
        session.MakeMove("hash-o", 4, now, now.AddHours(24));
        session.MakeMove("hash-x", 2, now, now.AddHours(24));
        return session;
    }

    private static DateTime Utc(int minutesOffset)
    {
        return new DateTime(2026, 2, 12, 0, minutesOffset, 0, DateTimeKind.Utc);
    }
}
