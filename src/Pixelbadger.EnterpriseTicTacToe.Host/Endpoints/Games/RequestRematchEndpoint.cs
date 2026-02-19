using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.SignalR;
using Pixelbadger.EnterpriseTicTacToe.Application;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.RequestRematch;
using Pixelbadger.EnterpriseTicTacToe.Host.Hubs;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class RequestRematchEndpoint(
    ISender mediator,
    IHubContext<GameHub> hubContext) : EndpointWithoutRequest<Result<GameStateDto>>
{
    public override void Configure()
    {
        Post("/{sessionCode}/rematch");
        Group<ResultGroup>();
        AllowAnonymous();
    }

    public override async Task<Result<GameStateDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var sessionCode = Route<string>("sessionCode") ?? string.Empty;
        var clientIdentity = HttpContext.GetRequiredClientIdentity();
        var result = await mediator.Send(new RequestRematchCommand(sessionCode, clientIdentity), cancellationToken);

        if (result.IsSuccess)
        {
            await hubContext.Clients.Group(result.Value.SessionCode)
                .SendAsync(RealtimeEvents.GameStateChanged, cancellationToken);
        }

        return result;
    }
}
