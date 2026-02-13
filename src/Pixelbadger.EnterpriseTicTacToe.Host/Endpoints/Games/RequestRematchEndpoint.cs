using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.SignalR;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.RequestRematch;
using Pixelbadger.EnterpriseTicTacToe.Host.Hubs;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class RequestRematchEndpoint(
    ISender mediator,
    IHubContext<GameHub> hubContext) : EndpointWithoutRequest<GameStateDto>
{
    public override void Configure()
    {
        Post("/api/games/{sessionCode}/rematch");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var sessionCode = Route<string>("sessionCode") ?? string.Empty;
        var clientIdentity = HttpContext.GetRequiredClientIdentity();
        var gameState = await mediator.Send(new RequestRematchCommand(sessionCode, clientIdentity), cancellationToken);

        await hubContext.Clients.Group(gameState.SessionCode)
            .SendAsync(RealtimeEvents.GameStateChanged, cancellationToken);

        await Send.OkAsync(gameState, cancellationToken);
    }
}
