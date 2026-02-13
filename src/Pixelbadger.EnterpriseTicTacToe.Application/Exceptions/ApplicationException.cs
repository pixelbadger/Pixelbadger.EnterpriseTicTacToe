namespace Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;

public class ApplicationException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
