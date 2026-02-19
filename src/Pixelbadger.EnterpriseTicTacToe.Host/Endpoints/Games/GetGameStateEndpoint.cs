using FastEndpoints;
using Mediator;
using Pixelbadger.EnterpriseTicTacToe.Application;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.GetGameState;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class GetGameStateEndpoint(ISender mediator) : EndpointWithoutRequest<Result<GameStateDto>>
{
    public override void Configure()
    {
        Get("/{sessionCode}");
        Group<ResultGroup>();
        AllowAnonymous();
    }

    public override async Task<Result<GameStateDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var sessionCode = Route<string>("sessionCode") ?? string.Empty;
        var clientIdentity = HttpContext.GetRequiredClientIdentity();
        return await mediator.Send(new GetGameStateQuery(sessionCode, clientIdentity), cancellationToken);
    }
}
