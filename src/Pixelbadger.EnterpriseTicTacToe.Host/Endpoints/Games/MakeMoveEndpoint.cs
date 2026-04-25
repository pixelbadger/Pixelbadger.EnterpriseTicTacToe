using FastEndpoints;
using Mediator;
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
    GameRealtimeNotifier realtimeNotifier) : Endpoint<MakeMoveRequest, GameStateDto>
{
    public override void Configure()
    {
        Post("/api/games/{sessionCode}/moves");
        AllowAnonymous();
    }

    public override async Task HandleAsync(MakeMoveRequest request, CancellationToken cancellationToken)
    {
        var sessionCode = Route<string>("sessionCode") ?? string.Empty;
        var clientIdentity = HttpContext.GetRequiredClientIdentity();
        var gameState = await mediator.Send(new MakeMoveCommand(sessionCode, request.CellIndex, clientIdentity), cancellationToken);

        await realtimeNotifier.BroadcastState(gameState.SessionCode, cancellationToken);

        await Send.OkAsync(gameState, cancellationToken);
    }
}
