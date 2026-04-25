using Mediator;
using Microsoft.AspNetCore.SignalR;
using Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.GetGameState;
using AppException = Pixelbadger.EnterpriseTicTacToe.Application.Exceptions.ApplicationException;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Hubs;

public sealed class GameRealtimeNotifier(
    ISender mediator,
    IHubContext<GameHub> hubContext,
    GameConnectionRegistry connectionRegistry,
    ILogger<GameRealtimeNotifier> logger)
{
    public async Task BroadcastState(string sessionCode, CancellationToken cancellationToken)
    {
        var connections = connectionRegistry.GetConnections(sessionCode);
        foreach (var connection in connections)
        {
            try
            {
                var state = await mediator.Send(
                    new GetGameStateQuery(sessionCode, connection.ClientIdentity),
                    cancellationToken);

                await hubContext.Clients.Client(connection.ConnectionId)
                    .SendAsync(RealtimeEvents.GameStateUpdated, state, cancellationToken);
            }
            catch (AppException exception)
            {
                logger.LogDebug(
                    exception,
                    "Unable to publish game state to connection {ConnectionId} for session {SessionCode}",
                    connection.ConnectionId,
                    sessionCode);
            }
        }
    }
}
