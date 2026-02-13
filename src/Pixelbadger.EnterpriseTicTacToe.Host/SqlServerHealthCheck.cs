using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Data;

namespace Pixelbadger.EnterpriseTicTacToe.Host;

public sealed class SqlServerHealthCheck(TicTacToeDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy("SQL Server connectivity is healthy.")
            : HealthCheckResult.Unhealthy("SQL Server connectivity check failed.");
    }
}
