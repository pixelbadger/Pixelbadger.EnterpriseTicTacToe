using FluentValidation;
using Mediator;
using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Application.Common.Mapping;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.SetPresence;

public sealed record SetPresenceCommand(string SessionCode, bool IsOnline, string ClientIdentity) : ICommand<Result<GameStateDto>>;

public sealed class SetPresenceCommandValidator : AbstractValidator<SetPresenceCommand>
{
    public SetPresenceCommandValidator()
    {
        RuleFor(request => request.SessionCode)
            .NotEmpty()
            .Length(6)
            .Must(GameStateMapper.IsValidCode);

        RuleFor(request => request.ClientIdentity)
            .NotEmpty();
    }
}

public sealed class SetPresenceCommandHandler(
    IGameSessionRepository gameSessionRepository,
    IUnitOfWork unitOfWork,
    IClientIdentityHasher clientIdentityHasher,
    IClock clock,
    IOptions<GameSessionSettings> settings)
    : ICommandHandler<SetPresenceCommand, Result<GameStateDto>>
{
    public async ValueTask<Result<GameStateDto>> Handle(SetPresenceCommand command, CancellationToken cancellationToken)
    {
        var normalizedCode = GameStateMapper.NormalizeCode(command.SessionCode);
        var session = await gameSessionRepository.GetByCode(normalizedCode, cancellationToken);
        if (session is null)
        {
            return Result.Failure<GameStateDto>(ResultErrorType.NotFound, "Game session was not found.");
        }

        var now = clock.UtcNow;
        if (session.IsExpired(now))
        {
            session.MarkExpired(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<GameStateDto>(ResultErrorType.NotFound, "Game session has expired.");
        }

        var identityHash = clientIdentityHasher.Hash(command.ClientIdentity);
        var ttlHours = Math.Max(1, settings.Value.InactivityTimeoutHours);
        var expiresAt = GameStateMapper.CalculateExpiry(now, ttlHours);

        try
        {
            session.SetPresence(identityHash, command.IsOnline, now, expiresAt);
        }
        catch (DomainRuleViolationException exception)
        {
            return Result.Failure<GameStateDto>(ResultErrorType.Forbidden, exception.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(GameStateMapper.ToDto(session, identityHash));
    }
}
