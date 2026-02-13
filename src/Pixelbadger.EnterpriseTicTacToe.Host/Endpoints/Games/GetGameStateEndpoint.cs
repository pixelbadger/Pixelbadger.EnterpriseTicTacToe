using FastEndpoints;
using Mediator;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.GetGameState;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class GetGameStateEndpoint(ISender mediator) : EndpointWithoutRequest<GameStateDto>
{
    public override void Configure()
    {
        Get("/api/games/{sessionCode}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var sessionCode = Route<string>("sessionCode") ?? string.Empty;
        var clientIdentity = HttpContext.GetRequiredClientIdentity();
        var gameState = await mediator.Send(new GetGameStateQuery(sessionCode, clientIdentity), cancellationToken);
        await Send.OkAsync(gameState, cancellationToken);
    }
}
