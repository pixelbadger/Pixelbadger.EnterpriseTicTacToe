using FastEndpoints;
using Mediator;
using Microsoft.AspNetCore.SignalR;
using Pixelbadger.EnterpriseTicTacToe.Application;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.JoinGame;
using Pixelbadger.EnterpriseTicTacToe.Host.Hubs;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class JoinGameRequest
{
    public string SessionCode { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;
}

public sealed class JoinGameEndpoint(
    ISender mediator,
    IHubContext<GameHub> hubContext) : Endpoint<JoinGameRequest, Result<GameStateDto>>
{
    public override void Configure()
    {
        Post("/join");
        Group<ResultGroup>();
        AllowAnonymous();
    }

    public override async Task<Result<GameStateDto>> ExecuteAsync(JoinGameRequest request, CancellationToken cancellationToken)
    {
        var clientIdentity = HttpContext.GetRequiredClientIdentity();
        var result = await mediator.Send(
            new JoinGameCommand(request.SessionCode, request.Username, clientIdentity),
            cancellationToken);

        if (result.IsSuccess)
        {
            await hubContext.Clients.Group(result.Value.SessionCode)
                .SendAsync(RealtimeEvents.GameStateChanged, cancellationToken);
        }

        return result;
    }
}
