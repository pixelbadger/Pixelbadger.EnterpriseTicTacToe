using FastEndpoints;
using Mediator;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.GetGameState;
using Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Common;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class GetGameStateEndpoint(ISender mediator) : ResultEndpointWithoutRequest<GameStateDto>
{
    protected override void ConfigureEndpoint()
    {
        Get("/api/games/{sessionCode}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var sessionCode = Route<string>("sessionCode") ?? string.Empty;
        var clientIdentity = HttpContext.GetRequiredClientIdentity();
        var result = await mediator.Send(new GetGameStateQuery(sessionCode, clientIdentity), cancellationToken);
        await SendResultAsync(result, cancellationToken);
    }
}
