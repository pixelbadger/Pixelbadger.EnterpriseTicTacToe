using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Pixelbadger.EnterpriseTicTacToe.Application;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure;
using Pixelbadger.EnterpriseTicTacToe.Host.Hubs;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;
using Microsoft.AspNetCore.Mvc;

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

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Request validation failed",
                validationException.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        grouping => grouping.Key,
                        grouping => grouping.Select(error => error.ErrorMessage).ToArray())),
            Pixelbadger.EnterpriseTicTacToe.Application.Exceptions.ApplicationException applicationException => (
                applicationException.StatusCode,
                applicationException.Message,
                new Dictionary<string, string[]>()),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Game state changed. Please retry.",
                new Dictionary<string, string[]>()),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                new Dictionary<string, string[]>())
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var details = new ValidationProblemDetails(errors)
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        await context.Response.WriteAsJsonAsync(details);
    });
});

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
