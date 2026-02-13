namespace Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;

public sealed class ForbiddenException(string message) : ApplicationException(message, 403);
