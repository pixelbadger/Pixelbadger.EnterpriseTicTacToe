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

            if (!string.IsNullOrEmpty(errorMessage))
            {
                throw new ArgumentException("Successful results cannot have an error message.", nameof(errorMessage));
            }
        }
        else
        {
            if (errorType == ResultErrorType.None)
            {
                throw new ArgumentException("Failed results must include an error type.", nameof(errorType));
            }

            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Failed results must include an error message.", nameof(errorMessage));
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

    public static Result Success() => new(isSuccess: true, errorType: ResultErrorType.None, errorMessage: string.Empty);

    public static Result Failure(ResultErrorType errorType, string errorMessage) =>
        new(isSuccess: false, errorType: errorType, errorMessage: errorMessage);

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    public static Result<TValue> Failure<TValue>(ResultErrorType errorType, string errorMessage) =>
        Result<TValue>.Failure(errorType, errorMessage);
}

public sealed class Result<TValue> : Result
{
    private Result(bool isSuccess, TValue? value, ResultErrorType errorType, string errorMessage)
        : base(isSuccess, errorType, errorMessage)
    {
        Value = value;
    }

    public TValue? Value { get; }

    public static Result<TValue> Success(TValue value) =>
        new(isSuccess: true, value: value, errorType: ResultErrorType.None, errorMessage: string.Empty);

    public static new Result<TValue> Failure(ResultErrorType errorType, string errorMessage) =>
        new(isSuccess: false, value: default, errorType: errorType, errorMessage: errorMessage);
}
