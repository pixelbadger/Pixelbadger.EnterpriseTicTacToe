var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sql");

var ticTacToeDb = sqlServer.AddDatabase("tic-tac-toe-db", "EnterpriseTicTacToe");

var api = builder.AddProject(
        name: "api",
        projectPath: "../Pixelbadger.EnterpriseTicTacToe.Host/Pixelbadger.EnterpriseTicTacToe.Host.csproj")
    .WithReference(ticTacToeDb)
    .WithExternalHttpEndpoints()
    .WaitFor(ticTacToeDb);

builder.AddViteApp("frontend", "../../frontend")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
