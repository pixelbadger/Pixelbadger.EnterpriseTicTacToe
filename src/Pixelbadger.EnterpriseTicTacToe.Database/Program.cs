using System.Reflection;
using DbUp;

var connectionString = ResolveConnectionString(args);

EnsureDatabase.For.SqlDatabase(connectionString);

var upgrader = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(
        Assembly.GetExecutingAssembly(),
        scriptName => scriptName.Contains(".Scripts.", StringComparison.Ordinal))
    .WithTransactionPerScript()
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();
if (!result.Successful)
{
    Console.Error.WriteLine(result.Error);
    return 1;
}

Console.WriteLine("Database schema migrations completed successfully.");
return 0;

static string ResolveConnectionString(string[] args)
{
    if (args.Length == 1 && !args[0].StartsWith("--", StringComparison.Ordinal))
    {
        return args[0];
    }

    for (var index = 0; index < args.Length; index++)
    {
        if (!string.Equals(args[index], "--connection-string", StringComparison.Ordinal))
        {
            continue;
        }

        if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
        {
            throw new InvalidOperationException("The --connection-string argument requires a non-empty value.");
        }

        return args[index + 1];
    }

    return Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? Environment.GetEnvironmentVariable("DefaultConnection")
        ?? Environment.GetEnvironmentVariable("TICTACTOE_DB_CONNECTION_STRING")
        ?? throw new InvalidOperationException(
            "No connection string provided. Pass one as the first argument, use --connection-string <value>, " +
            "or set ConnectionStrings__DefaultConnection/DefaultConnection/TICTACTOE_DB_CONNECTION_STRING.");
}
