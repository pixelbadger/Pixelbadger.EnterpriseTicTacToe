namespace Pixelbadger.EnterpriseTicTacToe.Domain.Services;

public interface Clock
{
    DateTime UtcNow { get; }
}
