using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Tests.Support;

internal sealed class InMemoryGameSessionRepository : IGameSessionRepository
{
    private readonly Dictionary<string, GameSession> _sessions = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, GameSession> Sessions => _sessions;

    public int AddCallCount { get; private set; }

    public int GetByCodeCallCount { get; private set; }

    public Task<GameSession?> GetByCode(string sessionCode, CancellationToken cancellationToken)
    {
        GetByCodeCallCount++;
        _sessions.TryGetValue(sessionCode, out var session);
        return Task.FromResult(session);
    }

    public Task Add(GameSession session, CancellationToken cancellationToken)
    {
        AddCallCount++;
        _sessions[session.SessionCode] = session;
        return Task.CompletedTask;
    }

    public void Seed(GameSession session)
    {
        _sessions[session.SessionCode] = session;
    }
}

internal sealed class RecordingUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingGameSessionCache : IGameSessionCache
{
    private readonly Dictionary<string, GameSession> _sessions = new(StringComparer.Ordinal);

    public int SetCallCount { get; private set; }

    public int RemoveCallCount { get; private set; }

    public bool TryGet(string sessionCode, out GameSession session)
    {
        return _sessions.TryGetValue(sessionCode, out session!);
    }

    public void Set(GameSession session)
    {
        SetCallCount++;
        _sessions[session.SessionCode] = session;
    }

    public void Remove(string sessionCode)
    {
        RemoveCallCount++;
        _sessions.Remove(sessionCode);
    }

    public void Seed(GameSession session)
    {
        _sessions[session.SessionCode] = session;
    }
}

internal sealed class NoOpGameSessionLock : IGameSessionLock
{
    public ValueTask<IAsyncDisposable> AcquireAsync(string sessionCode, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<IAsyncDisposable>(NoOpLease.Instance);
    }

    private sealed class NoOpLease : IAsyncDisposable
    {
        public static readonly NoOpLease Instance = new();

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}

internal sealed class RecordingGamePresenceTracker : IGamePresenceTracker
{
    private readonly HashSet<string> _onlinePlayers = new(StringComparer.Ordinal);

    public int SetPresenceCallCount { get; private set; }

    public void SetPresence(string sessionCode, string clientIdentityHash, bool isOnline)
    {
        SetPresenceCallCount++;
        var key = CreateKey(sessionCode, clientIdentityHash);
        if (isOnline)
        {
            _onlinePlayers.Add(key);
            return;
        }

        _onlinePlayers.Remove(key);
    }

    public bool IsOnline(string sessionCode, string clientIdentityHash)
    {
        return _onlinePlayers.Contains(CreateKey(sessionCode, clientIdentityHash));
    }

    private static string CreateKey(string sessionCode, string clientIdentityHash)
    {
        return $"{sessionCode}:{clientIdentityHash}";
    }
}

internal sealed class AdjustableClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; set; } = utcNow;
}

internal sealed class PrefixClientIdentityHasher : IClientIdentityHasher
{
    public string Hash(string clientIdentity)
    {
        return $"hash::{clientIdentity}";
    }
}

internal sealed class QueueSessionCodeGenerator(IEnumerable<string> codes) : ISessionCodeGenerator
{
    private readonly Queue<string> _codes = new(codes);

    public string GenerateCode()
    {
        if (_codes.Count == 0)
        {
            throw new InvalidOperationException("No session code values are left in this test generator.");
        }

        return _codes.Dequeue();
    }
}

internal sealed class ConstantSessionCodeGenerator(string code) : ISessionCodeGenerator
{
    public string GenerateCode() => code;
}

internal static class GameSessionFactory
{
    public static GameSession CreateWaitingSession(
        string sessionCode = "ABC123",
        string username = "Host",
        string clientIdentity = "cookie-1",
        DateTime? utcNow = null,
        int inactivityTimeoutHours = 24)
    {
        var now = utcNow ?? new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        return GameSession.Create(
            sessionCode: sessionCode,
            username: username,
            normalizedUsername: username.Trim().ToUpperInvariant(),
            clientIdentityHash: $"hash::{clientIdentity}",
            utcNow: now,
            expiresAtUtc: now.AddHours(inactivityTimeoutHours));
    }

    public static GameSession CreateInProgressSession(
        string sessionCode = "ABC123",
        DateTime? utcNow = null,
        int inactivityTimeoutHours = 24)
    {
        var now = utcNow ?? new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var session = CreateWaitingSession(sessionCode, utcNow: now, inactivityTimeoutHours: inactivityTimeoutHours);
        session.JoinOpponent("Guest", "GUEST", "hash::cookie-2", now, now.AddHours(inactivityTimeoutHours));
        return session;
    }

    public static GameSession CreateCompletedSession(
        string sessionCode = "ABC123",
        DateTime? utcNow = null,
        int inactivityTimeoutHours = 24)
    {
        var now = utcNow ?? new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc);
        var session = CreateInProgressSession(sessionCode, now, inactivityTimeoutHours);
        session.MakeMove("hash::cookie-1", 0, now, now.AddHours(inactivityTimeoutHours));
        session.MakeMove("hash::cookie-2", 3, now, now.AddHours(inactivityTimeoutHours));
        session.MakeMove("hash::cookie-1", 1, now, now.AddHours(inactivityTimeoutHours));
        session.MakeMove("hash::cookie-2", 4, now, now.AddHours(inactivityTimeoutHours));
        session.MakeMove("hash::cookie-1", 2, now, now.AddHours(inactivityTimeoutHours));
        return session;
    }
}
