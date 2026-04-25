using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Common.Mapping;

internal static class GameStateMapper
{
    public static GameStateDto ToDto(
        GameSession session,
        string currentIdentityHash,
        Func<string, bool>? isOnline = null)
    {
        return new GameStateDto(
            SessionCode: session.SessionCode,
            JoinPath: $"/join/{session.SessionCode}",
            Status: session.Status.ToString(),
            BoardState: session.BoardState,
            CurrentTurn: session.CurrentTurn?.ToString(),
            Winner: session.Winner?.ToString(),
            RematchXReady: session.RematchXReady,
            RematchOReady: session.RematchOReady,
            PlayerCount: session.PlayerCount,
            Players: session.Players
                .OrderBy(player => player.Mark)
                .Select(player => new PlayerStateDto(
                    Username: player.Username,
                    Mark: player.Mark.ToString(),
                    IsOnline: isOnline?.Invoke(player.ClientIdentityHash) ?? player.IsOnline,
                    IsCurrentPlayer: player.ClientIdentityHash == currentIdentityHash))
                .ToArray(),
            LastActivityUtc: session.LastActivityUtc);
    }

    public static string NormalizeUsername(string username)
    {
        return username.Trim().ToUpperInvariant();
    }

    public static bool IsValidCode(string sessionCode)
    {
        return sessionCode.Length == GameSession.SessionCodeLength && sessionCode.All(char.IsAsciiLetterOrDigit);
    }

    public static string NormalizeCode(string sessionCode)
    {
        return sessionCode.Trim().ToUpperInvariant();
    }

    public static DateTime CalculateExpiry(DateTime utcNow, int inactivityTimeoutHours)
    {
        return utcNow.AddHours(inactivityTimeoutHours);
    }

    public static bool CanRejoin(PlayerMark mark, string username, string normalizedUsername, string currentIdentityHash, GameSession session)
    {
        var player = session.FindPlayerByNormalizedUsername(normalizedUsername);
        if (player is null)
        {
            return false;
        }

        return player.Mark == mark && player.Username == username && player.ClientIdentityHash == currentIdentityHash;
    }
}
