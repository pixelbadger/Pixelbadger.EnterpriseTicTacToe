using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Data;

namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure.Services;

internal sealed class InactiveSessionCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<GameSessionSettings> settings,
    ILogger<InactiveSessionCleanupService> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TicTacToeDbContext>();
                var now = DateTime.UtcNow;
                var inactivityHours = Math.Max(1, settings.Value.InactivityTimeoutHours);
                var cutoff = now.AddHours(-inactivityHours);

                var changedRows = await dbContext.GameSessions
                    .Where(session =>
                        session.Status != GameStatus.Expired &&
                        session.LastActivityUtc <= cutoff)
                    .ExecuteUpdateAsync(updates => updates
                            .SetProperty(session => session.Status, GameStatus.Expired)
                            .SetProperty(session => session.CurrentTurn, (Domain.Enums.PlayerMark?)null)
                            .SetProperty(session => session.Winner, (Domain.Enums.PlayerMark?)null)
                            .SetProperty(session => session.UpdatedUtc, now)
                            .SetProperty(session => session.LastActivityUtc, now)
                            .SetProperty(session => session.ExpiresAtUtc, now),
                        stoppingToken);

                if (changedRows > 0)
                {
                    logger.LogInformation("Expired {ExpiredSessions} inactive game sessions.", changedRows);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed while expiring inactive game sessions.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
