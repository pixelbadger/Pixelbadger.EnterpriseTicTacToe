using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using AppException = Pixelbadger.EnterpriseTicTacToe.Application.Exceptions.ApplicationException;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.GetGameState;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.SetPresence;
using Pixelbadger.EnterpriseTicTacToe.Host.Middleware;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Hubs;

public sealed class GameHub(
    ISender mediator,
    GameConnectionRegistry connectionRegistry,
    GameRealtimeNotifier realtimeNotifier,
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
        connectionRegistry.Track(Context.ConnectionId, normalizedCode, clientIdentity);

        await SetPresenceWithConcurrencyFallback(normalizedCode, true, clientIdentity, cancellationToken);
        await realtimeNotifier.BroadcastState(normalizedCode, cancellationToken);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (connectionRegistry.TryRemove(Context.ConnectionId, out var connection))
        {
            try
            {
                if (!connectionRegistry.HasActiveIdentity(connection.SessionCode, connection.ClientIdentity))
                {
                    await SetPresenceWithConcurrencyFallback(
                        connection.SessionCode,
                        false,
                        connection.ClientIdentity,
                        CancellationToken.None);
                }

                await realtimeNotifier.BroadcastState(connection.SessionCode, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Unable to set player presence offline for session {SessionCode}", connection.SessionCode);
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
        var gameState = await mediator.Send(new GetGameStateQuery(normalizedCode, clientIdentity), cancellationToken);
        await Clients.Caller.SendAsync(RealtimeEvents.GameStateUpdated, gameState, cancellationToken);
    }

    private async Task<GameStateDto> SetPresenceWithConcurrencyFallback(
        string sessionCode,
        bool isOnline,
        string clientIdentity,
        CancellationToken cancellationToken)
    {
        try
        {
            return await mediator.Send(new SetPresenceCommand(sessionCode, isOnline, clientIdentity), cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogDebug(exception, "Presence update conflict for session {SessionCode}", sessionCode);
            try
            {
                return await mediator.Send(new GetGameStateQuery(sessionCode, clientIdentity), cancellationToken);
            }
            catch (AppException appException)
            {
                logger.LogInformation(appException, "Presence refresh failed after concurrency conflict for session {SessionCode}", sessionCode);
                throw new HubException(appException.Message);
            }
        }
        catch (AppException exception)
        {
            logger.LogInformation(exception, "Presence update rejected for session {SessionCode}", sessionCode);
            throw new HubException(exception.Message);
        }
    }
}
