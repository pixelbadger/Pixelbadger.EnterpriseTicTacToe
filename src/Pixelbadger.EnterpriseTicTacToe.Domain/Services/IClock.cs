namespace Pixelbadger.EnterpriseTicTacToe.Domain.Services;

public interface IClock
{
    DateTime UtcNow { get; }
}
