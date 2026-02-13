using Pixelbadger.EnterpriseTicTacToe.Domain.Services;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Data;

namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(TicTacToeDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
