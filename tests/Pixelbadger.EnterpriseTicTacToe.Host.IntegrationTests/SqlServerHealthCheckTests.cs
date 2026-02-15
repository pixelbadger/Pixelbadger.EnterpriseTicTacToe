using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Data;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Host.IntegrationTests;

[TestClass]
public sealed class SqlServerHealthCheckTests
{
    [TestMethod]
    public async Task CheckHealthAsync_ReturnsHealthyWhenDbContextCanConnect()
    {
        var dbOptions = new DbContextOptionsBuilder<TicTacToeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var dbContext = new TicTacToeDbContext(dbOptions);
        var healthCheck = new Pixelbadger.EnterpriseTicTacToe.Host.SqlServerHealthCheck(dbContext);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }
}
