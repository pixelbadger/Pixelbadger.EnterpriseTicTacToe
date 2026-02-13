using Azure.Provisioning.AppService;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AppService;

var builder = DistributedApplication.CreateBuilder(args);

var clientIdentityHashKey = builder.AddParameter("client-identity-hash-key", secret: true);

var sqlServer = builder.AddAzureSqlServer("sql")
    .RunAsContainer();
var ticTacToeDb = sqlServer.AddDatabase("tictactoedb", "tictactoe");

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
    api.WithEnvironment("AZURE_TOKEN_CREDENTIALS", "prod");

    builder.AddAzureAppServiceEnvironment("app-service-env")
        .ConfigureInfrastructure(infra =>
        {
            var resources = infra.GetProvisionableResources();
            var appServicePlan = resources.OfType<AppServicePlan>().Single();
            appServicePlan.Sku = new AppServiceSkuDescription
            {
                Name = "B1",
                Tier = "Basic"
            };
        });

    api.PublishAsAzureAppServiceWebsite((_, website) =>
    {
        website.SiteConfig.NumberOfWorkers = 1;
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
