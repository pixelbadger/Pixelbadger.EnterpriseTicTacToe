using FastEndpoints;
using Mediator;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.StartGame;
using Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Common;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class StartGameRequest
{
    public string Username { get; init; } = string.Empty;
}

public sealed class StartGameEndpoint(ISender mediator) : ResultEndpoint<StartGameRequest, GameStateDto>
{
    protected override void ConfigureEndpoint()
    {
        Post("/api/games/start");
        AllowAnonymous();
    }

    public override async Task HandleAsync(StartGameRequest request, CancellationToken cancellationToken)
    {
        var clientIdentity = HttpContext.GetRequiredClientIdentity();
        var result = await mediator.Send(new StartGameCommand(request.Username, clientIdentity), cancellationToken);
        await SendResultAsync(result, cancellationToken);
    }
}
