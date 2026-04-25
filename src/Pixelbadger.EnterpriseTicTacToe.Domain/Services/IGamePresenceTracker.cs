namespace Pixelbadger.EnterpriseTicTacToe.Domain.Services;

public interface IGamePresenceTracker
{
    void SetPresence(string sessionCode, string clientIdentityHash, bool isOnline);

    bool IsOnline(string sessionCode, string clientIdentityHash);
}
