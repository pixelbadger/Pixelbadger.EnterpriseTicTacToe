using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Host.IntegrationTests.Middleware;

[TestClass]
public sealed class ClientIdentityCookieMiddlewareTests
{
    [TestMethod]
    public async Task InvokeAsync_WhenCookieMissing_GeneratesIdentityAndSetsCookie()
    {
        var nextCalled = false;
        var context = new DefaultHttpContext();
        var middleware = new ClientIdentityCookieMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new StubHostEnvironment(isDevelopment: true));

        await middleware.InvokeAsync(context);

        nextCalled.ShouldBeTrue();
        context.Items[ClientIdentityCookieMiddleware.HttpContextItemName].ShouldNotBeNull();
        context.Response.Headers.SetCookie.Count.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenCookieValid_UsesExistingIdentity()
    {
        var existingIdentity = new string('a', 32);
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{ClientIdentityCookieMiddleware.CookieName}={existingIdentity}";
        var middleware = new ClientIdentityCookieMiddleware(_ => Task.CompletedTask, new StubHostEnvironment(isDevelopment: false));

        await middleware.InvokeAsync(context);

        context.Items[ClientIdentityCookieMiddleware.HttpContextItemName].ShouldBe(existingIdentity);
        context.Response.Headers.SetCookie.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task InvokeAsync_WhenCookieTooShort_ReplacesCookieValue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{ClientIdentityCookieMiddleware.CookieName}=short";
        var middleware = new ClientIdentityCookieMiddleware(_ => Task.CompletedTask, new StubHostEnvironment(isDevelopment: true));

        await middleware.InvokeAsync(context);

        context.Items[ClientIdentityCookieMiddleware.HttpContextItemName].ShouldNotBe("short");
        context.Response.Headers.SetCookie.Count.ShouldBeGreaterThan(0);
    }

    private sealed class StubHostEnvironment(bool isDevelopment) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = isDevelopment ? "Development" : "Production";

        public string ApplicationName { get; set; } = "Tests";

        public string ContentRootPath { get; set; } = "/";

        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
