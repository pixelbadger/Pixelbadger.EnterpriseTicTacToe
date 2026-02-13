namespace Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;

public sealed class ConflictException(string message) : ApplicationException(message, 409);
