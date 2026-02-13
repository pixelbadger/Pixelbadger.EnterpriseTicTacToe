using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Configuration;

namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure.Services;

internal sealed class HmacClientIdentityHasher(IOptions<ClientIdentityOptions> options) : ClientIdentityHasher
{
    private readonly byte[] _keyBytes = Encoding.UTF8.GetBytes(options.Value.HashKey);

    public string Hash(string clientIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientIdentity);

        using var hmac = new HMACSHA256(_keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(clientIdentity));
        return Convert.ToBase64String(hash);
    }
}
