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

        registry.Track("connection-1", "ABC123");

        var found = registry.TryGetSessionCode("connection-1", out var sessionCode);

        found.ShouldBeTrue();
        sessionCode.ShouldBe("ABC123");
    }

    [TestMethod]
    public void TryRemove_RemovesConnectionFromRegistry()
    {
        var registry = new GameConnectionRegistry();
        registry.Track("connection-1", "ABC123");

        var removed = registry.TryRemove("connection-1", out var removedCode);
        var stillPresent = registry.TryGetSessionCode("connection-1", out _);

        removed.ShouldBeTrue();
        removedCode.ShouldBe("ABC123");
        stillPresent.ShouldBeFalse();
    }
}
