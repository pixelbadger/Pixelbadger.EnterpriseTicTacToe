namespace Pixelbadger.EnterpriseTicTacToe.Domain.Services;

public interface IGameSessionLock
{
    ValueTask<IAsyncDisposable> AcquireAsync(string sessionCode, CancellationToken cancellationToken);
}
