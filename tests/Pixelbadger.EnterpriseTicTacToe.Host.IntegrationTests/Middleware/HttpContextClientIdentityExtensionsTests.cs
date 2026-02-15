using Microsoft.AspNetCore.Http;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Host.IntegrationTests.Middleware;

[TestClass]
public sealed class HttpContextClientIdentityExtensionsTests
{
    [TestMethod]
    public void GetRequiredClientIdentity_WhenItemPresent_ReturnsItemValue()
    {
        var context = new DefaultHttpContext();
        context.Items[ClientIdentityCookieMiddleware.HttpContextItemName] = "item-identity";
        context.Request.Headers.Cookie = $"{ClientIdentityCookieMiddleware.CookieName}=cookie-identity";

        var identity = context.GetRequiredClientIdentity();

        identity.ShouldBe("item-identity");
    }

    [TestMethod]
    public void GetRequiredClientIdentity_WhenItemMissing_ReturnsCookieValue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{ClientIdentityCookieMiddleware.CookieName}=cookie-identity";

        var identity = context.GetRequiredClientIdentity();

        identity.ShouldBe("cookie-identity");
    }

    [TestMethod]
    public void GetRequiredClientIdentity_WhenMissingEverywhere_Throws()
    {
        var context = new DefaultHttpContext();

        Should.Throw<InvalidOperationException>(() => context.GetRequiredClientIdentity());
    }
}
