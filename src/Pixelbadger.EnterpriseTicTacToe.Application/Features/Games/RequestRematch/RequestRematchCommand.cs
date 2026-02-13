using FluentValidation;
using Mediator;
using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Application.Common.Mapping;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.RequestRematch;

public sealed record RequestRematchCommand(string SessionCode, string ClientIdentity) : ICommand<GameStateDto>;

public sealed class RequestRematchCommandValidator : AbstractValidator<RequestRematchCommand>
{
    public RequestRematchCommandValidator()
    {
        RuleFor(request => request.SessionCode)
            .NotEmpty()
            .Length(6)
            .Must(GameStateMapper.IsValidCode);

        RuleFor(request => request.ClientIdentity)
            .NotEmpty();
    }
}

public sealed class RequestRematchCommandHandler(
    IGameSessionRepository gameSessionRepository,
    IClientIdentityHasher clientIdentityHasher,
    IClock clock,
    IOptions<GameSessionSettings> settings)
    : ICommandHandler<RequestRematchCommand, GameStateDto>
{
    public async ValueTask<GameStateDto> Handle(RequestRematchCommand command, CancellationToken cancellationToken)
    {
        var normalizedCode = GameStateMapper.NormalizeCode(command.SessionCode);
        var session = await gameSessionRepository.GetByCode(normalizedCode, cancellationToken)
            ?? throw new NotFoundException("Game session was not found.");

        var now = clock.UtcNow;
        if (session.IsExpired(now))
        {
            session.MarkExpired(now);
            await gameSessionRepository.SaveChanges(cancellationToken);
            throw new NotFoundException("Game session has expired.");
        }

        var identityHash = clientIdentityHasher.Hash(command.ClientIdentity);
        var ttlHours = Math.Max(1, settings.Value.InactivityTimeoutHours);
        var expiresAt = GameStateMapper.CalculateExpiry(now, ttlHours);

        try
        {
            session.RegisterRematchVote(identityHash, now, expiresAt);
        }
        catch (DomainRuleViolationException exception)
        {
            throw new ConflictException(exception.Message);
        }

        await gameSessionRepository.SaveChanges(cancellationToken);
        return GameStateMapper.ToDto(session, identityHash);
    }
}
