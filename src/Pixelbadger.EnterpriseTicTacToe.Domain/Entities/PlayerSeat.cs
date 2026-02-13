using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;

namespace Pixelbadger.EnterpriseTicTacToe.Domain.Entities;

public sealed class PlayerSeat
{
    private PlayerSeat()
    {
    }

    public Guid Id { get; private set; }

    public Guid GameSessionId { get; private set; }

    public GameSession? GameSession { get; private set; }

    public string Username { get; private set; } = string.Empty;

    public string NormalizedUsername { get; private set; } = string.Empty;

    public string ClientIdentityHash { get; private set; } = string.Empty;

    public PlayerMark Mark { get; private set; }

    public bool IsOnline { get; private set; }

    public DateTime JoinedUtc { get; private set; }

    public DateTime LastSeenUtc { get; private set; }

    public static PlayerSeat Create(
        PlayerMark mark,
        string username,
        string normalizedUsername,
        string clientIdentityHash,
        DateTime utcNow,
        bool isOnline = true)
    {
        return new PlayerSeat
        {
            Id = Guid.NewGuid(),
            Mark = mark,
            Username = username,
            NormalizedUsername = normalizedUsername,
            ClientIdentityHash = clientIdentityHash,
            IsOnline = isOnline,
            JoinedUtc = utcNow,
            LastSeenUtc = utcNow
        };
    }

    public void SetPresence(bool isOnline, DateTime utcNow)
    {
        IsOnline = isOnline;
        LastSeenUtc = utcNow;
    }
}
