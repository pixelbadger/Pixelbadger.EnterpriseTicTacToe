namespace Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;

public sealed class NotFoundException(string message) : ApplicationException(message, 404);
