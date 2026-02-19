using FastEndpoints;
using FastEndpoints.Swagger;
using Pixelbadger.EnterpriseTicTacToe.Application;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure;
using Pixelbadger.EnterpriseTicTacToe.Host.Hubs;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddMediator(options =>
{
    options.Assemblies = [typeof(Pixelbadger.EnterpriseTicTacToe.Application.DependencyInjection).Assembly];
    options.ServiceLifetime = ServiceLifetime.Scoped;
});
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(document =>
{
    document.DocumentSettings = settings =>
    {
        settings.Title = "Enterprise Tic-Tac-Toe API";
        settings.Version = "v1";
    };
});
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
builder.Services.AddSingleton<GameConnectionRegistry>();
builder.Services.AddHealthChecks()
    .AddCheck<Pixelbadger.EnterpriseTicTacToe.Host.SqlServerHealthCheck>("sqlserver");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseMiddleware<ClientIdentityCookieMiddleware>();
app.UseFastEndpoints();
app.MapHub<GameHub>("/hubs/game");
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

var webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (Directory.Exists(webRoot))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
}

app.Run();

public partial class Program;
