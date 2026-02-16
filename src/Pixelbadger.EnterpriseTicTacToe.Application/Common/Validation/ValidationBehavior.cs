using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Pixelbadger.EnterpriseTicTacToe.Application;

public sealed class ValidationBehavior<TMessage, TResponse>(IServiceProvider serviceProvider)
    : IPipelineBehavior<TMessage, Result<TResponse>>
    where TMessage : IMessage
{
    public async ValueTask<Result<TResponse>> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, Result<TResponse>> next,
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
            var messageText = string.Join(
                Environment.NewLine,
                validationResult.Errors
                    .Select(error => $"{error.PropertyName}: {error.ErrorMessage}")
                    .Distinct(StringComparer.Ordinal));

            if (string.IsNullOrWhiteSpace(messageText))
            {
                messageText = "Request validation failed.";
            }

            return Result.Failure<TResponse>(ResultErrorType.Invalid, messageText);
        }

        return await next(message, cancellationToken);
    }
}
