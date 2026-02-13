using Microsoft.EntityFrameworkCore;
using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Data;

namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure.Persistence;

internal sealed class EfGameSessionRepository(TicTacToeDbContext dbContext) : IGameSessionRepository
{
    public async Task<GameSession?> GetByCode(string sessionCode, CancellationToken cancellationToken)
    {
        return await dbContext.GameSessions
            .Include(session => session.Players)
            .SingleOrDefaultAsync(session => session.SessionCode == sessionCode, cancellationToken);
    }

    public Task Add(GameSession session, CancellationToken cancellationToken)
    {
        return dbContext.GameSessions.AddAsync(session, cancellationToken).AsTask();
    }

    public Task SaveChanges(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
