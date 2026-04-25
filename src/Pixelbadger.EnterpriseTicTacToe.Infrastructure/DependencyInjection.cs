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
    private const int MinimumClientIdentityHashKeyLength = 32;
    private const string DevelopmentHashKey = "development-only-please-change-me";

    private static readonly string[] ProductionPlaceholderHashKeys =
    [
        "replace-me-with-env-configured-key",
        "CHANGE_ME_USE_ENVIRONMENT_SECRET"
    ];

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        bool allowDevelopmentHashKey = false)
    {
        services.Configure<GameSessionSettings>(configuration.GetSection(GameSessionSettings.SectionName));
        services.AddOptions<ClientIdentityOptions>()
            .Bind(configuration.GetSection(ClientIdentityOptions.SectionName))
            .Validate(options => IsValidClientIdentityHashKey(options.HashKey, allowDevelopmentHashKey),
                $"{ClientIdentityOptions.SectionName}:HashKey must be a non-placeholder value at least {MinimumClientIdentityHashKeyLength} characters long.")
            .ValidateOnStart();

        services.AddDbContext<TicTacToeDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure();
            });
        });

        services.AddScoped<IGameSessionRepository, EfGameSessionRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISessionCodeGenerator, RandomSessionCodeGenerator>();
        services.AddScoped<IClientIdentityHasher, HmacClientIdentityHasher>();
        services.AddHostedService<InactiveSessionCleanupService>();

        return services;
    }

    private static bool IsValidClientIdentityHashKey(string hashKey, bool allowDevelopmentHashKey)
    {
        if (string.IsNullOrWhiteSpace(hashKey) || hashKey.Length < MinimumClientIdentityHashKeyLength)
        {
            return false;
        }

        if (ProductionPlaceholderHashKeys.Contains(hashKey, StringComparer.Ordinal))
        {
            return false;
        }

        return allowDevelopmentHashKey || !string.Equals(hashKey, DevelopmentHashKey, StringComparison.Ordinal);
    }
}
