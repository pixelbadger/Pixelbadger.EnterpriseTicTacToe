using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Data;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Persistence;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GameSessionSettings>(configuration.GetSection(GameSessionSettings.SectionName));
        services.Configure<ClientIdentityOptions>(configuration.GetSection(ClientIdentityOptions.SectionName));

        services.AddDbContext<TicTacToeDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure();
            });
        });

        services.AddScoped<GameSessionRepository, EfGameSessionRepository>();
        services.AddSingleton<Clock, SystemClock>();
        services.AddSingleton<SessionCodeGenerator, RandomSessionCodeGenerator>();
        services.AddScoped<ClientIdentityHasher, HmacClientIdentityHasher>();
        services.AddHostedService<InactiveSessionCleanupService>();

        return services;
    }
}
