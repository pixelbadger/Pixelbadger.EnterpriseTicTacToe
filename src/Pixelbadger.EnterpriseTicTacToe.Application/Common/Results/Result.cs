using System.Reflection;

namespace Pixelbadger.EnterpriseTicTacToe.Application;

public class Result
{
    protected Result(bool isSuccess, ResultErrorType errorType, string errorMessage)
    {
        if (isSuccess)
        {
            if (errorType != ResultErrorType.None)
            {
                throw new ArgumentException("Successful results cannot have an error type.", nameof(errorType));
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Successful results cannot have an error message.", nameof(errorMessage));
            }
        }
        else
        {
            if (errorType == ResultErrorType.None)
            {
                throw new ArgumentException("Failed results must specify an error type.", nameof(errorType));
            }

            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Failed results must specify an error message.", nameof(errorMessage));
            }
        }

        IsSuccess = isSuccess;
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ResultErrorType ErrorType { get; }

    public string ErrorMessage { get; }

    public static Result Success()
    {
        return new Result(isSuccess: true, ResultErrorType.None, string.Empty);
    }

    public static Result Failure(ResultErrorType errorType, string errorMessage)
    {
        return new Result(isSuccess: false, errorType, errorMessage);
    }

    public static Result<TValue> Success<TValue>(TValue value)
    {
        return Result<TValue>.Success(value);
    }

    public static Result<TValue> Failure<TValue>(ResultErrorType errorType, string errorMessage)
    {
        return Result<TValue>.Failure(errorType, errorMessage);
    }

    internal static TResponse CreateFailure<TResponse>(ResultErrorType errorType, string errorMessage)
        where TResponse : Result
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Failure(errorType, errorMessage);
        }

        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(TResponse).GenericTypeArguments[0];
            var method = typeof(Result)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(candidate => candidate.Name == nameof(Failure) && candidate.IsGenericMethodDefinition)
                .MakeGenericMethod(valueType);

            return (TResponse)method.Invoke(null, [errorType, errorMessage])!;
        }

        throw new InvalidOperationException($"Response type '{typeof(TResponse).Name}' is not supported by result factories.");
    }
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue? value, bool isSuccess, ResultErrorType errorType, string errorMessage)
        : base(isSuccess, errorType, errorMessage)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value for a failed result.");

    public static Result<TValue> Success(TValue value)
    {
        return new Result<TValue>(value, isSuccess: true, ResultErrorType.None, string.Empty);
    }

    public static new Result<TValue> Failure(ResultErrorType errorType, string errorMessage)
    {
        return new Result<TValue>(default, isSuccess: false, errorType, errorMessage);
    }
}
