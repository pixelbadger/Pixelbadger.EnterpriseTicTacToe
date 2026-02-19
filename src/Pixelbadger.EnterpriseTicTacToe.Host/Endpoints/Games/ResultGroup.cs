using FastEndpoints;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class ResultGroup : Group
{
    public ResultGroup()
    {
        Configure("api/games", endpointDefinition =>
        {
            endpointDefinition.DontAutoSendResponse();
            endpointDefinition.PostProcessors(Order.After, typeof(ResultPostProcessor<,>));
        });
    }
}
