using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Domain.Tests;

[TestClass]
public sealed class PlayerSeatTests
{
    [TestMethod]
    public void Create_AssignsExpectedFields()
    {
        var now = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);

        var player = PlayerSeat.Create(PlayerMark.O, "Guest", "GUEST", "hash-o", now, isOnline: false);

        player.Id.ShouldNotBe(Guid.Empty);
        player.Mark.ShouldBe(PlayerMark.O);
        player.Username.ShouldBe("Guest");
        player.NormalizedUsername.ShouldBe("GUEST");
        player.ClientIdentityHash.ShouldBe("hash-o");
        player.IsOnline.ShouldBeFalse();
        player.JoinedUtc.ShouldBe(now);
        player.LastSeenUtc.ShouldBe(now);
    }

    [TestMethod]
    public void SetPresence_UpdatesOnlineAndLastSeen()
    {
        var now = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var updateTime = now.AddMinutes(5);
        var player = PlayerSeat.Create(PlayerMark.X, "Host", "HOST", "hash-x", now);

        player.SetPresence(false, updateTime);

        player.IsOnline.ShouldBeFalse();
        player.LastSeenUtc.ShouldBe(updateTime);
    }
}
