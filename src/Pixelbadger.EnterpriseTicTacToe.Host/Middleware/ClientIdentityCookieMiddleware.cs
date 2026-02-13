using System.Security.Cryptography;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

internal sealed class ClientIdentityCookieMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    public const string CookieName = "ttt_client";
    public const string HttpContextItemName = "ClientIdentity";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var clientIdentity) ||
            string.IsNullOrWhiteSpace(clientIdentity) ||
            clientIdentity.Length < 32)
        {
            clientIdentity = GenerateIdentity();
            context.Response.Cookies.Append(CookieName, clientIdentity, new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment() || context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/",
                MaxAge = TimeSpan.FromDays(30)
            });
        }

        context.Items[HttpContextItemName] = clientIdentity;
        await next(context);
    }

    private static string GenerateIdentity()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
