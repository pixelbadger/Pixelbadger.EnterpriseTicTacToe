using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.SignalR;
using Pixelbadger.EnterpriseTicTacToe.Application;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.MakeMove;
using Pixelbadger.EnterpriseTicTacToe.Host.Hubs;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class MakeMoveRequest
{
    public int CellIndex { get; init; }
}

public sealed class MakeMoveEndpoint(
    ISender mediator,
    IHubContext<GameHub> hubContext) : Endpoint<MakeMoveRequest, Result<GameStateDto>>
{
    public override void Configure()
    {
        Post("/{sessionCode}/moves");
        Group<ResultGroup>();
        AllowAnonymous();
    }

    public override async Task<Result<GameStateDto>> ExecuteAsync(MakeMoveRequest request, CancellationToken cancellationToken)
    {
        var sessionCode = Route<string>("sessionCode") ?? string.Empty;
        var clientIdentity = HttpContext.GetRequiredClientIdentity();
        var result = await mediator.Send(new MakeMoveCommand(sessionCode, request.CellIndex, clientIdentity), cancellationToken);

        if (result.IsSuccess)
        {
            await hubContext.Clients.Group(result.Value.SessionCode)
                .SendAsync(RealtimeEvents.GameStateChanged, cancellationToken);
        }

        return result;
    }
}
