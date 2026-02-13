namespace Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

internal static class HttpContextClientIdentityExtensions
{
    public static string GetRequiredClientIdentity(this HttpContext context)
    {
        if (context.Items.TryGetValue(ClientIdentityCookieMiddleware.HttpContextItemName, out var value) &&
            value is string clientIdentity &&
            !string.IsNullOrWhiteSpace(clientIdentity))
        {
            return clientIdentity;
        }

        if (context.Request.Cookies.TryGetValue(ClientIdentityCookieMiddleware.CookieName, out var cookieValue) &&
            !string.IsNullOrWhiteSpace(cookieValue))
        {
            return cookieValue;
        }

        throw new InvalidOperationException("Anonymous client identity is missing.");
    }
}
