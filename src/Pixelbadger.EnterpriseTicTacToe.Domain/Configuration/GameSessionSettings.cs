namespace Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;

public sealed class GameSessionSettings
{
    public const string SectionName = "GameSession";

    public int InactivityTimeoutHours { get; init; } = 24;
}
