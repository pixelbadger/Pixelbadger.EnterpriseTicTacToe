namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure.Configuration;

public sealed class ClientIdentityOptions
{
    public const string SectionName = "ClientIdentity";

    public string HashKey { get; init; } = "replace-me-with-env-configured-key";
}
