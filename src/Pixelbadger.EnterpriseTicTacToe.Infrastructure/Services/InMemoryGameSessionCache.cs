using System.Collections.Concurrent;
using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure.Services;

internal sealed class InMemoryGameSessionCache : IGameSessionCache
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new(StringComparer.Ordinal);

    public bool TryGet(string sessionCode, out GameSession session)
    {
        return _sessions.TryGetValue(sessionCode, out session!);
    }

    public void Set(GameSession session)
    {
        _sessions[session.SessionCode] = session;
    }

    public void Remove(string sessionCode)
    {
        _sessions.TryRemove(sessionCode, out _);
    }
}
