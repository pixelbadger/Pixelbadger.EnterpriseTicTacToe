using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure.Services;

internal sealed class SystemClock : Clock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
