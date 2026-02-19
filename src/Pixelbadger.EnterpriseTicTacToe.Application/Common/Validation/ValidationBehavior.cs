using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Pixelbadger.EnterpriseTicTacToe.Application;

public sealed class ValidationBehavior<TMessage, TResponse>(IServiceProvider serviceProvider)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : Result
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

        var validationResult = await validator.ValidateAsync(message, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errorMessage = string.Join(
                Environment.NewLine,
                validationResult.Errors
                    .Select(error => $"{error.PropertyName}: {error.ErrorMessage}"));

            return Result.CreateFailure<TResponse>(ResultErrorType.Invalid, errorMessage);
        }

        return await next(message, cancellationToken);
    }
}
