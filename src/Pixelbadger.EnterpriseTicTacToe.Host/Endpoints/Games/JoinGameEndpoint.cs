using FastEndpoints;
using Mediator;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.JoinGame;
using Pixelbadger.EnterpriseTicTacToe.Host.Hubs;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class JoinGameRequest
{
    public string SessionCode { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;
}

public sealed class JoinGameEndpoint(
    ISender mediator,
    GameRealtimeNotifier realtimeNotifier) : Endpoint<JoinGameRequest, GameStateDto>
{
    public override void Configure()
    {
        Post("/api/games/join");
        AllowAnonymous();
    }

    public override async Task HandleAsync(JoinGameRequest request, CancellationToken cancellationToken)
    {
        var clientIdentity = HttpContext.GetRequiredClientIdentity();
        var gameState = await mediator.Send(
            new JoinGameCommand(request.SessionCode, request.Username, clientIdentity),
            cancellationToken);

        await realtimeNotifier.BroadcastState(gameState.SessionCode, cancellationToken);

        await Send.OkAsync(gameState, cancellationToken);
    }
}
