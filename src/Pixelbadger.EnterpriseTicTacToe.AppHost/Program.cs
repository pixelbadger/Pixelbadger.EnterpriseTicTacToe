if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME")))
{
    Environment.SetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME", "podman");
}

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject(
        name: "api",
        projectPath: "../Pixelbadger.EnterpriseTicTacToe.Host/Pixelbadger.EnterpriseTicTacToe.Host.csproj")
    .WithExternalHttpEndpoints();

builder.AddViteApp("frontend", "../../frontend")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
