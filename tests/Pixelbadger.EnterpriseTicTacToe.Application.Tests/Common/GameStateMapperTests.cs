using Pixelbadger.EnterpriseTicTacToe.Application.Common.Mapping;
using Pixelbadger.EnterpriseTicTacToe.Application.Tests.Support;
using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Tests.Common;

[TestClass]
public sealed class GameStateMapperTests
{
    [TestMethod]
    public void ToDto_MapsGameStateWithOrderedPlayers()
    {
        var session = GameSessionFactory.CreateInProgressSession();

        var dto = GameStateMapper.ToDto(session, "hash::cookie-2");

        dto.SessionCode.ShouldBe("ABC123");
        dto.JoinPath.ShouldBe("/join/ABC123");
        dto.Players.Count.ShouldBe(2);
        dto.Players[0].Mark.ShouldBe("X");
        dto.Players[1].Mark.ShouldBe("O");
        dto.Players[1].IsCurrentPlayer.ShouldBeTrue();
    }

    [TestMethod]
    public void NormalizeUsername_TrimsAndUppercases()
    {
        GameStateMapper.NormalizeUsername("  host User  ").ShouldBe("HOST USER");
    }

    [TestMethod]
    public void IsValidCode_RequiresConfiguredLengthAsciiAlphanumericCharacters()
    {
        GameStateMapper.IsValidCode("ABC12345").ShouldBeTrue();
        GameStateMapper.IsValidCode("abc12345").ShouldBeTrue();
        GameStateMapper.IsValidCode("ABC123").ShouldBeFalse();
        GameStateMapper.IsValidCode("ABC1234!").ShouldBeFalse();
    }

    [TestMethod]
    public void NormalizeCode_TrimsAndUppercases()
    {
        GameStateMapper.NormalizeCode(" ab12cd ").ShouldBe("AB12CD");
    }

    [TestMethod]
    public void CalculateExpiry_AddsConfiguredHours()
    {
        var now = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);

        var expiry = GameStateMapper.CalculateExpiry(now, 4);

        expiry.ShouldBe(now.AddHours(4));
    }

    [TestMethod]
    public void CanRejoin_ReturnsTrueOnlyWhenAllIdentityFieldsMatch()
    {
        var session = GameSessionFactory.CreateInProgressSession();

        GameStateMapper.CanRejoin(PlayerMark.X, "Host", "HOST", "hash::cookie-1", session).ShouldBeTrue();
        GameStateMapper.CanRejoin(PlayerMark.X, "Host", "HOST", "hash::cookie-2", session).ShouldBeFalse();
        GameStateMapper.CanRejoin(PlayerMark.O, "Guest", "GUEST", "hash::cookie-2", session).ShouldBeTrue();
        GameStateMapper.CanRejoin(PlayerMark.O, "Guest", "OTHER", "hash::cookie-2", session).ShouldBeFalse();
    }
}
