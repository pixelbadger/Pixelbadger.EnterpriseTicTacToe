using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;

namespace Pixelbadger.EnterpriseTicTacToe.Domain.Services;

public interface IGameSessionCache
{
    bool TryGet(string sessionCode, out GameSession session);

    void Set(GameSession session);

    void Remove(string sessionCode);
}
