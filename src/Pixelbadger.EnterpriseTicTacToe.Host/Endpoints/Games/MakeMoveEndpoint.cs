using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.SignalR;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.MakeMove;
using Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Common;
using Pixelbadger.EnterpriseTicTacToe.Host.Hubs;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class MakeMoveRequest
{
    public int CellIndex { get; init; }
}

public sealed class MakeMoveEndpoint(
    ISender mediator,
    IHubContext<GameHub> hubContext) : ResultEndpoint<MakeMoveRequest, GameStateDto>
{
    protected override void ConfigureEndpoint()
    {
        Post("/api/games/{sessionCode}/moves");
        AllowAnonymous();
    }

    public override async Task HandleAsync(MakeMoveRequest request, CancellationToken cancellationToken)
    {
        var sessionCode = Route<string>("sessionCode") ?? string.Empty;
        var clientIdentity = HttpContext.GetRequiredClientIdentity();
        var result = await mediator.Send(new MakeMoveCommand(sessionCode, request.CellIndex, clientIdentity), cancellationToken);

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
