using FastEndpoints;
using Pixelbadger.EnterpriseTicTacToe.Application;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Pixelbadger.EnterpriseTicTacToe.Host.Endpoints.Games;

public sealed class ResultPostProcessor<TRequest, TResponse> : IPostProcessor<TRequest, Result<TResponse>>
    where TRequest : notnull
{
    public async Task PostProcessAsync(IPostProcessorContext<TRequest, Result<TResponse>> context, CancellationToken cancellationToken)
    {
        if (context.HttpContext.ResponseStarted())
        {
            return;
        }

        var result = context.Response;
        if (result is null)
        {
            return;
        }

        if (result.IsSuccess)
        {
            await context.HttpContext.Response.WriteAsJsonAsync(result.Value, cancellationToken);
            return;
        }

        var statusCode = ToStatusCode(result.ErrorType);
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ToTitle(result.ErrorType),
            Detail = result.ErrorMessage,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = context.HttpContext.Request.Path
        };

        context.HttpContext.Response.StatusCode = statusCode;
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
    }

    private static int ToStatusCode(ResultErrorType errorType)
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

    private static string ToTitle(ResultErrorType errorType)
    {
        return errorType switch
        {
            ResultErrorType.Invalid => "Request validation failed",
            ResultErrorType.Forbidden => "Forbidden",
            ResultErrorType.NotFound => "Not Found",
            ResultErrorType.Conflict => "Conflict",
            _ => "An unexpected error occurred"
        };
    }
}
