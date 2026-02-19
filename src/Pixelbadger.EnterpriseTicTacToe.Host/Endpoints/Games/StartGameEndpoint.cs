using FastEndpoints;
using Mediator;
using Pixelbadger.EnterpriseTicTacToe.Application;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.StartGame;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class StartGameRequest
{
    public string Username { get; init; } = string.Empty;
}

public sealed class StartGameEndpoint(ISender mediator) : Endpoint<StartGameRequest, Result<GameStateDto>>
{
    public override void Configure()
    {
        Post("/start");
        Group<ResultGroup>();
        AllowAnonymous();
    }

    public override async Task<Result<GameStateDto>> ExecuteAsync(StartGameRequest request, CancellationToken cancellationToken)
    {
        var clientIdentity = HttpContext.GetRequiredClientIdentity();
        return await mediator.Send(new StartGameCommand(request.Username, clientIdentity), cancellationToken);
    }
}
