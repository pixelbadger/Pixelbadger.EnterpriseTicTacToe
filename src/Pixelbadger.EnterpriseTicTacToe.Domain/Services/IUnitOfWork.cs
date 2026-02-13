namespace Pixelbadger.EnterpriseTicTacToe.Domain.Services;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
