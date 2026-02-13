namespace Pixelbadger.EnterpriseTicTacToe.Domain.Exceptions;

public sealed class DomainRuleViolationException(string message) : Exception(message);
