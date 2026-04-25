using System.Collections.Concurrent;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Hubs;

public sealed class GameConnectionRegistry
{
    private readonly ConcurrentDictionary<string, GameConnection> _connections = new(StringComparer.Ordinal);

    public void Track(string connectionId, string sessionCode, string clientIdentity)
    {
        _connections[connectionId] = new GameConnection(connectionId, sessionCode, clientIdentity);
    }

    public bool TryGetSessionCode(string connectionId, out string sessionCode)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            sessionCode = connection.SessionCode;
            return true;
        }

        sessionCode = string.Empty;
        return false;
    }

    public IReadOnlyList<GameConnection> GetConnections(string sessionCode)
    {
        return _connections.Values
            .Where(connection => string.Equals(connection.SessionCode, sessionCode, StringComparison.Ordinal))
            .ToArray();
    }

    public bool HasActiveIdentity(string sessionCode, string clientIdentity)
    {
        return _connections.Values.Any(connection =>
            string.Equals(connection.SessionCode, sessionCode, StringComparison.Ordinal) &&
            string.Equals(connection.ClientIdentity, clientIdentity, StringComparison.Ordinal));
    }

    public bool TryRemove(string connectionId, out GameConnection connection)
    {
        return _connections.TryRemove(connectionId, out connection!);
    }
}

public sealed record GameConnection(string ConnectionId, string SessionCode, string ClientIdentity);
