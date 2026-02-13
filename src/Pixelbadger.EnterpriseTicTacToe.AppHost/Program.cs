var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sql");
var ticTacToeDb = sqlServer.AddDatabase("tic-tac-toe-db", "tictactoe");

builder.AddProject(
        name: "dbup",
        projectPath: "../Pixelbadger.EnterpriseTicTacToe.Database/Pixelbadger.EnterpriseTicTacToe.Database.csproj")
    .WithReference(ticTacToeDb)
    .WithEnvironment("ConnectionStrings__DefaultConnection", ticTacToeDb.Resource.ConnectionStringExpression)
    .WaitFor(ticTacToeDb)
    .WithExplicitStart();

var api = builder.AddProject(
        name: "api",
        projectPath: "../Pixelbadger.EnterpriseTicTacToe.Host/Pixelbadger.EnterpriseTicTacToe.Host.csproj")
    .WithReference(ticTacToeDb)
    .WithEnvironment("ConnectionStrings__DefaultConnection", ticTacToeDb.Resource.ConnectionStringExpression)
    .WaitFor(ticTacToeDb)
    .WithExternalHttpEndpoints();

builder.AddViteApp("frontend", "../../frontend")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
