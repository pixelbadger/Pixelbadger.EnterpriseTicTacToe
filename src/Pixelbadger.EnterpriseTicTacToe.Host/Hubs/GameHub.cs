using Mediator;
using Microsoft.AspNetCore.SignalR;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.GetGameState;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.SetPresence;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Hubs;

public sealed class GameHub(
    ISender mediator,
    GameConnectionRegistry connectionRegistry,
    ILogger<GameHub> logger) : Hub
{
    public async Task JoinSession(string sessionCode, CancellationToken cancellationToken)
    {
        var httpContext = Context.GetHttpContext()
            ?? throw new HubException("Unable to access HTTP context for hub connection.");

        var clientIdentity = httpContext.GetRequiredClientIdentity();
        var normalizedCode = sessionCode.Trim().ToUpperInvariant();

        await Groups.AddToGroupAsync(Context.ConnectionId, normalizedCode, cancellationToken);
        connectionRegistry.Track(Context.ConnectionId, normalizedCode);

        var gameState = await mediator.Send(new SetPresenceCommand(normalizedCode, true, clientIdentity), cancellationToken);
        await Clients.Group(normalizedCode).SendAsync(RealtimeEvents.GameStateUpdated, gameState, cancellationToken);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (connectionRegistry.TryRemove(Context.ConnectionId, out var sessionCode))
        {
            try
            {
                var httpContext = Context.GetHttpContext();
                if (httpContext is not null)
                {
                    var clientIdentity = httpContext.GetRequiredClientIdentity();
                    var gameState = await mediator.Send(new SetPresenceCommand(sessionCode, false, clientIdentity), CancellationToken.None);
                    await Clients.Group(sessionCode).SendAsync(RealtimeEvents.GameStateUpdated, gameState, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Unable to set player presence offline for session {SessionCode}", sessionCode);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task RefreshState(string sessionCode, CancellationToken cancellationToken)
    {
        var httpContext = Context.GetHttpContext()
            ?? throw new HubException("Unable to access HTTP context for hub connection.");

        var clientIdentity = httpContext.GetRequiredClientIdentity();
        var normalizedCode = sessionCode.Trim().ToUpperInvariant();
        var gameState = await mediator.Send(new GetGameStateQuery(normalizedCode, clientIdentity), cancellationToken);
        await Clients.Caller.SendAsync(RealtimeEvents.GameStateUpdated, gameState, cancellationToken);
    }
}
