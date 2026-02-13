using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;

namespace Pixelbadger.EnterpriseTicTacToe.Domain.Services;

public interface GameSessionRepository
{
    Task<GameSession?> GetByCode(string sessionCode, CancellationToken cancellationToken);

    Task Add(GameSession session, CancellationToken cancellationToken);

    Task SaveChanges(CancellationToken cancellationToken);
}
