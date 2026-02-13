using System.Collections.Concurrent;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Hubs;

public sealed class GameConnectionRegistry
{
    private readonly ConcurrentDictionary<string, string> _connections = new();

    public void Track(string connectionId, string sessionCode)
    {
        _connections[connectionId] = sessionCode;
    }

    public bool TryGetSessionCode(string connectionId, out string sessionCode)
    {
        return _connections.TryGetValue(connectionId, out sessionCode!);
    }

    public bool TryRemove(string connectionId, out string sessionCode)
    {
        return _connections.TryRemove(connectionId, out sessionCode!);
    }
}
