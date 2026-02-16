using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Pixelbadger.EnterpriseTicTacToe.Application;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Common;

public sealed class ResultPostProcessor<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
    where TRequest : notnull
{
    internal static readonly object FailedResultItemKey = new();

    public async Task PostProcessAsync(IPostProcessorContext<TRequest, TResponse> context, CancellationToken cancellationToken)
    {
        if (context.HttpContext.Response.HasStarted)
        {
            return;
        }

        if (!context.HttpContext.Items.TryGetValue(FailedResultItemKey, out var resultObject)
            || resultObject is not Result result
            || result.IsSuccess)
        {
            return;
        }

        context.HttpContext.Items.Remove(FailedResultItemKey);

        var statusCode = ToStatusCode(result.ErrorType);

        var details = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = ToTitle(result.ErrorType),
            Detail = result.ErrorMessage,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        context.HttpContext.Response.StatusCode = statusCode;
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(details, cancellationToken: cancellationToken);
    }

    internal static int ToStatusCode(ResultErrorType errorType)
    {
        return errorType switch
        {
            ResultErrorType.Invalid => StatusCodes.Status400BadRequest,
            ResultErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ResultErrorType.NotFound => StatusCodes.Status404NotFound,
            ResultErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    internal static string ToTitle(ResultErrorType errorType)
    {
        return errorType switch
        {
            ResultErrorType.Invalid => "Request validation failed",
            ResultErrorType.Forbidden => "Forbidden",
            ResultErrorType.NotFound => "Not found",
            ResultErrorType.Conflict => "Conflict",
            _ => "An unexpected error occurred"
        };
    }
}
