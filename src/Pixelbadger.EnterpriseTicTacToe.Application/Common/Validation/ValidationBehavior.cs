using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Pixelbadger.EnterpriseTicTacToe.Application;

public sealed class ValidationBehavior<TMessage, TResponse>(IServiceProvider serviceProvider)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var validator = serviceProvider.GetService<IValidator<TMessage>>();
        if (validator is null)
        {
            return await next(message, cancellationToken);
        }

        await validator.ValidateAndThrowAsync(message, cancellationToken);
        return await next(message, cancellationToken);
    }
}
