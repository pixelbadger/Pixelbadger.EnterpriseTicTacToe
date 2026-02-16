using FastEndpoints;
using Pixelbadger.EnterpriseTicTacToe.Application;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Common;

public abstract class ResultEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    public sealed override void Configure()
    {
        ConfigureEndpoint();
        PostProcessor<ResultPostProcessor<TRequest, TResponse>>();
    }

    protected abstract void ConfigureEndpoint();

    protected async Task SendResultAsync(Result<TResponse> result, CancellationToken cancellationToken)
    {
        if (result.IsSuccess)
        {
            await Send.OkAsync(result.Value!, cancellationToken);
            return;
        }

        HttpContext.Items[ResultPostProcessor<TRequest, TResponse>.FailedResultItemKey] = result;
    }
}

public abstract class ResultEndpointWithoutRequest<TResponse> : EndpointWithoutRequest<TResponse>
{
    public sealed override void Configure()
    {
        ConfigureEndpoint();
        PostProcessor<ResultPostProcessor<EmptyRequest, TResponse>>();
    }

    protected abstract void ConfigureEndpoint();

    protected async Task SendResultAsync(Result<TResponse> result, CancellationToken cancellationToken)
    {
        if (result.IsSuccess)
        {
            await Send.OkAsync(result.Value!, cancellationToken);
            return;
        }

        HttpContext.Items[ResultPostProcessor<EmptyRequest, TResponse>.FailedResultItemKey] = result;
    }
}
