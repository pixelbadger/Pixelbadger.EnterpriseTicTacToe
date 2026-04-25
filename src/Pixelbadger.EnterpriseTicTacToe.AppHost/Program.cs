using Azure.Provisioning.AppService;
using Azure.Provisioning.Sql;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AppService;

var builder = DistributedApplication.CreateBuilder(args);

var clientIdentityHashKey = builder.AddParameter("client-identity-hash-key", secret: true);

var sqlServer = builder.AddAzureSqlServer("sql")
    .ClearDefaultRoleAssignments()
    .ConfigureInfrastructure(infra =>
    {
        var resources = infra.GetProvisionableResources();

        foreach (var server in resources.OfType<SqlServer>())
        {
            server.PublicNetworkAccess = ServerNetworkAccessFlag.Disabled;
        }

        foreach (var firewallRule in resources.OfType<SqlFirewallRule>().ToArray())
        {
            infra.Remove(firewallRule);
        }
    })
    .RunAsContainer();
var ticTacToeDb = sqlServer.AddDatabase("tictactoedb", "tictactoe")
    .WithDefaultAzureSku();

if (!builder.ExecutionContext.IsPublishMode)
{
    builder.AddProject(
            name: "dbup",
            projectPath: "../Pixelbadger.EnterpriseTicTacToe.Database/Pixelbadger.EnterpriseTicTacToe.Database.csproj")
        .WithReference(ticTacToeDb)
        .WithEnvironment("ConnectionStrings__DefaultConnection", ticTacToeDb.Resource.ConnectionStringExpression)
        .WaitFor(ticTacToeDb)
        .WithExplicitStart();
}

var api = builder.AddProject(
        name: "api",
        projectPath: "../Pixelbadger.EnterpriseTicTacToe.Host/Pixelbadger.EnterpriseTicTacToe.Host.csproj")
    .WithReference(ticTacToeDb)
    .WithEnvironment("ConnectionStrings__DefaultConnection", ticTacToeDb.Resource.ConnectionStringExpression)
    .WithEnvironment("ClientIdentity__HashKey", clientIdentityHashKey)
    .WaitFor(ticTacToeDb)
    .WithExternalHttpEndpoints();

if (builder.ExecutionContext.IsPublishMode)
{
    builder.AddAzureAppServiceEnvironment("app-service-env")
        .ConfigureInfrastructure(infra =>
        {
            var resources = infra.GetProvisionableResources();

            foreach (var sqlDatabase in resources.OfType<SqlDatabase>())
            {
                sqlDatabase.FreeLimitExhaustionBehavior = FreeLimitExhaustionBehavior.BillOverUsage;
            }

            var appServicePlan = resources.OfType<AppServicePlan>().Single();
            appServicePlan.Sku = new AppServiceSkuDescription
            {
                Name = "B1",
                Tier = "Basic"
            };
        });

    api.PublishAsAzureAppServiceWebsite((_, website) =>
    {
        website.IsHttpsOnly = true;
        website.SiteConfig.NumberOfWorkers = 1;

        var tokenCredentialsSetting = website.SiteConfig.AppSettings
            .FirstOrDefault(setting =>
                string.Equals(setting.Value?.Name?.Value, "AZURE_TOKEN_CREDENTIALS", StringComparison.Ordinal));

        if (tokenCredentialsSetting?.Value is not null)
        {
            tokenCredentialsSetting.Value.Value = "prod";
        }
    });
}
else
{
    builder.AddViteApp("frontend", "../../frontend")
        .WithReference(api)
        .WaitFor(api)
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
