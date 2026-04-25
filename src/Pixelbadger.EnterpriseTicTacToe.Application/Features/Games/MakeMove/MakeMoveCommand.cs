using FluentValidation;
using Mediator;
using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Application.Common.Mapping;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Domain.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.MakeMove;

public sealed record MakeMoveCommand(string SessionCode, int CellIndex, string ClientIdentity) : ICommand<GameStateDto>;

public sealed class MakeMoveCommandValidator : AbstractValidator<MakeMoveCommand>
{
    public MakeMoveCommandValidator()
    {
        RuleFor(request => request.SessionCode)
            .NotEmpty()
            .Length(GameSession.SessionCodeLength)
            .Must(GameStateMapper.IsValidCode);

        RuleFor(request => request.CellIndex)
            .InclusiveBetween(0, 8);

        RuleFor(request => request.ClientIdentity)
            .NotEmpty();
    }
}

public sealed class MakeMoveCommandHandler(
    IGameSessionRepository gameSessionRepository,
    IUnitOfWork unitOfWork,
    IGameSessionCache gameSessionCache,
    IGameSessionLock gameSessionLock,
    IGamePresenceTracker gamePresenceTracker,
    IClientIdentityHasher clientIdentityHasher,
    IClock clock,
    IOptions<GameSessionSettings> settings)
    : ICommandHandler<MakeMoveCommand, GameStateDto>
{
    public async ValueTask<GameStateDto> Handle(MakeMoveCommand command, CancellationToken cancellationToken)
    {
        var normalizedCode = GameStateMapper.NormalizeCode(command.SessionCode);
        await using var sessionLease = await gameSessionLock.AcquireAsync(normalizedCode, cancellationToken);

        var session = await gameSessionRepository.GetByCode(normalizedCode, cancellationToken)
            ?? throw new NotFoundException("Game session was not found.");

        var now = clock.UtcNow;
        if (session.IsExpired(now))
        {
            session.MarkExpired(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            gameSessionCache.Remove(normalizedCode);
            throw new NotFoundException("Game session has expired.");
        }

        var identityHash = clientIdentityHasher.Hash(command.ClientIdentity);
        var ttlHours = Math.Max(1, settings.Value.InactivityTimeoutHours);
        var expiresAt = GameStateMapper.CalculateExpiry(now, ttlHours);

        try
        {
            session.MakeMove(identityHash, command.CellIndex, now, expiresAt);
        }
        catch (DomainRuleViolationException exception)
        {
            throw new ConflictException(exception.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        gameSessionCache.Set(session);
        return GameStateMapper.ToDto(
            session,
            identityHash,
            playerIdentityHash => gamePresenceTracker.IsOnline(session.SessionCode, playerIdentityHash));
    }
}
