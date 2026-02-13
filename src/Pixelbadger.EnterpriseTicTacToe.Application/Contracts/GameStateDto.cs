namespace Pixelbadger.EnterpriseTicTacToe.Application.Contracts;

public sealed record PlayerStateDto(
    string Username,
    string Mark,
    bool IsOnline,
    bool IsCurrentPlayer);

public sealed record GameStateDto(
    string SessionCode,
    string JoinPath,
    string Status,
    string BoardState,
    string? CurrentTurn,
    string? Winner,
    bool RematchXReady,
    bool RematchOReady,
    int PlayerCount,
    IReadOnlyList<PlayerStateDto> Players,
    DateTime LastActivityUtc);
