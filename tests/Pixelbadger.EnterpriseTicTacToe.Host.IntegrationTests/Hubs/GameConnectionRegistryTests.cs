using Pixelbadger.EnterpriseTicTacToe.Host.Hubs;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Host.IntegrationTests.Hubs;

[TestClass]
public sealed class GameConnectionRegistryTests
{
    [TestMethod]
    public void Track_ThenTryGetSessionCode_ReturnsStoredSessionCode()
    {
        var registry = new GameConnectionRegistry();

        registry.Track("connection-1", "ABC123", "cookie-1");

        var found = registry.TryGetSessionCode("connection-1", out var sessionCode);

        found.ShouldBeTrue();
        sessionCode.ShouldBe("ABC123");
    }

    [TestMethod]
    public void TryRemove_RemovesConnectionFromRegistry()
    {
        var registry = new GameConnectionRegistry();
        registry.Track("connection-1", "ABC123", "cookie-1");

        var removed = registry.TryRemove("connection-1", out var removedConnection);
        var stillPresent = registry.TryGetSessionCode("connection-1", out _);

        removed.ShouldBeTrue();
        removedConnection.SessionCode.ShouldBe("ABC123");
        stillPresent.ShouldBeFalse();
    }

    [TestMethod]
    public void HasActiveIdentity_WhenAnotherConnectionRemains_ReturnsTrue()
    {
        var registry = new GameConnectionRegistry();
        registry.Track("connection-1", "ABC123", "cookie-1");
        registry.Track("connection-2", "ABC123", "cookie-1");

        registry.TryRemove("connection-1", out _).ShouldBeTrue();

        registry.HasActiveIdentity("ABC123", "cookie-1").ShouldBeTrue();
    }
}
