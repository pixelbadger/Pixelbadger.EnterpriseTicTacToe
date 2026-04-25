using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure;
using Pixelbadger.EnterpriseTicTacToe.Infrastructure.Configuration;
using Shouldly;

namespace Pixelbadger.EnterpriseTicTacToe.Host.IntegrationTests.Infrastructure.Configuration;

[TestClass]
public sealed class ClientIdentityOptionsValidationTests
{
    [TestMethod]
    public void HashKey_WhenProductionPlaceholder_ThrowsOptionsValidationException()
    {
        using var provider = BuildProvider("CHANGE_ME_USE_ENVIRONMENT_SECRET", allowDevelopmentHashKey: false);

        Should.Throw<OptionsValidationException>(() => _ = provider.GetRequiredService<IOptions<ClientIdentityOptions>>().Value);
    }

    [TestMethod]
    public void HashKey_WhenDevelopmentPlaceholderInProduction_ThrowsOptionsValidationException()
    {
        using var provider = BuildProvider("development-only-please-change-me", allowDevelopmentHashKey: false);

        Should.Throw<OptionsValidationException>(() => _ = provider.GetRequiredService<IOptions<ClientIdentityOptions>>().Value);
    }

    [TestMethod]
    public void HashKey_WhenDevelopmentPlaceholderInDevelopment_IsAllowed()
    {
        using var provider = BuildProvider("development-only-please-change-me", allowDevelopmentHashKey: true);

        var options = provider.GetRequiredService<IOptions<ClientIdentityOptions>>().Value;

        options.HashKey.ShouldBe("development-only-please-change-me");
    }

    [TestMethod]
    public void HashKey_WhenStrongSecret_IsAllowed()
    {
        const string strongSecret = "strong-production-secret-with-at-least-32-chars";
        using var provider = BuildProvider(strongSecret, allowDevelopmentHashKey: false);

        var options = provider.GetRequiredService<IOptions<ClientIdentityOptions>>().Value;

        options.HashKey.ShouldBe(strongSecret);
    }

    private static ServiceProvider BuildProvider(string hashKey, bool allowDevelopmentHashKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=tictactoe;Trusted_Connection=True;TrustServerCertificate=True",
                [$"{ClientIdentityOptions.SectionName}:HashKey"] = hashKey
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructureServices(configuration, allowDevelopmentHashKey);
        return services.BuildServiceProvider();
    }
}
