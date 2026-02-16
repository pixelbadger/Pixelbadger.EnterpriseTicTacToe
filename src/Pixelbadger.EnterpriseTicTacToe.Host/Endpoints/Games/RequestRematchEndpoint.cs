using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.SignalR;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.RequestRematch;
using Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Common;
using Pixelbadger.EnterpriseTicTacToe.Host.Hubs;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class RequestRematchEndpoint(
    ISender mediator,
    IHubContext<GameHub> hubContext) : ResultEndpointWithoutRequest<GameStateDto>
{
    protected override void ConfigureEndpoint()
    {
        Post("/api/games/{sessionCode}/rematch");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var sessionCode = Route<string>("sessionCode") ?? string.Empty;
        var clientIdentity = HttpContext.GetRequiredClientIdentity();
        var result = await mediator.Send(new RequestRematchCommand(sessionCode, clientIdentity), cancellationToken);

        if (!result.IsSuccess)
        {
            await SendResultAsync(result, cancellationToken);
            return;
        }

        await hubContext.Clients.Group(result.Value!.SessionCode)
            .SendAsync(RealtimeEvents.GameStateChanged, cancellationToken);

        await SendResultAsync(result, cancellationToken);
    }
}
