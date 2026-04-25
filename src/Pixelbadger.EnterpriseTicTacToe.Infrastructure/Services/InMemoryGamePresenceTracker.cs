using System.Collections.Concurrent;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure.Services;

internal sealed class InMemoryGamePresenceTracker : IGamePresenceTracker
{
    private readonly ConcurrentDictionary<string, byte> _onlinePlayers = new(StringComparer.Ordinal);

    public void SetPresence(string sessionCode, string clientIdentityHash, bool isOnline)
    {
        var key = CreateKey(sessionCode, clientIdentityHash);
        if (isOnline)
        {
            _onlinePlayers[key] = 0;
            return;
        }

        _onlinePlayers.TryRemove(key, out _);
    }

    public bool IsOnline(string sessionCode, string clientIdentityHash)
    {
        return _onlinePlayers.ContainsKey(CreateKey(sessionCode, clientIdentityHash));
    }

    private static string CreateKey(string sessionCode, string clientIdentityHash)
    {
        return $"{sessionCode}:{clientIdentityHash}";
    }
}
