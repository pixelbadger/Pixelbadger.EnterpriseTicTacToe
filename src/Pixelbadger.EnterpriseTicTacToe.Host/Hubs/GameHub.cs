using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Pixelbadger.EnterpriseTicTacToe.Application;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.GetGameState;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.SetPresence;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Hubs;

public sealed class GameHub(
    ISender mediator,
    GameConnectionRegistry connectionRegistry,
    ILogger<GameHub> logger) : Hub
{
    public async Task JoinSession(string sessionCode)
    {
        var cancellationToken = Context.ConnectionAborted;
        var httpContext = Context.GetHttpContext()
            ?? throw new HubException("Unable to access HTTP context for hub connection.");

        var clientIdentity = httpContext.GetRequiredClientIdentity();
        var normalizedCode = sessionCode.Trim().ToUpperInvariant();

        await Groups.AddToGroupAsync(Context.ConnectionId, normalizedCode, cancellationToken);
        connectionRegistry.Track(Context.ConnectionId, normalizedCode);

        var gameState = await SetPresenceWithConcurrencyFallback(normalizedCode, true, clientIdentity, cancellationToken);
        await Clients.Caller.SendAsync(RealtimeEvents.GameStateUpdated, gameState, cancellationToken);
        await Clients.OthersInGroup(normalizedCode).SendAsync(RealtimeEvents.GameStateChanged, cancellationToken);
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
                    await SetPresenceWithConcurrencyFallback(sessionCode, false, clientIdentity, CancellationToken.None);
                    await Clients.Group(sessionCode).SendAsync(RealtimeEvents.GameStateChanged, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Unable to set player presence offline for session {SessionCode}", sessionCode);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task RefreshState(string sessionCode)
    {
        var cancellationToken = Context.ConnectionAborted;
        var httpContext = Context.GetHttpContext()
            ?? throw new HubException("Unable to access HTTP context for hub connection.");

        var clientIdentity = httpContext.GetRequiredClientIdentity();
        var normalizedCode = sessionCode.Trim().ToUpperInvariant();
        var result = await mediator.Send(new GetGameStateQuery(normalizedCode, clientIdentity), cancellationToken);
        if (result.IsFailure)
        {
            throw new HubException(result.ErrorMessage);
        }

        await Clients.Caller.SendAsync(RealtimeEvents.GameStateUpdated, result.Value, cancellationToken);
    }

    private async Task<GameStateDto> SetPresenceWithConcurrencyFallback(
        string sessionCode,
        bool isOnline,
        string clientIdentity,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await mediator.Send(new SetPresenceCommand(sessionCode, isOnline, clientIdentity), cancellationToken);
            if (result.IsSuccess)
            {
                return result.Value;
            }

            logger.LogInformation(
                "Presence update rejected for session {SessionCode} with error type {ErrorType}",
                sessionCode,
                result.ErrorType);

            throw new HubException(result.ErrorMessage);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogDebug(exception, "Presence update conflict for session {SessionCode}", sessionCode);
            var refreshResult = await mediator.Send(new GetGameStateQuery(sessionCode, clientIdentity), cancellationToken);
            if (refreshResult.IsSuccess)
            {
                return refreshResult.Value;
            }

            logger.LogInformation(
                "Presence refresh failed after concurrency conflict for session {SessionCode} with error type {ErrorType}",
                sessionCode,
                refreshResult.ErrorType);

            throw new HubException(refreshResult.ErrorMessage);
        }
    }
}
