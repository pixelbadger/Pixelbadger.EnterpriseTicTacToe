var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sql");

var ticTacToeDb = sqlServer.AddDatabase("tic-tac-toe-db", "EnterpriseTicTacToe");

builder.AddProject(
        name: "api",
        projectPath: "../Pixelbadger.EnterpriseTicTacToe.Host/Pixelbadger.EnterpriseTicTacToe.Host.csproj")
    .WithReference(ticTacToeDb)
    .WaitFor(ticTacToeDb);

builder.Build().Run();
