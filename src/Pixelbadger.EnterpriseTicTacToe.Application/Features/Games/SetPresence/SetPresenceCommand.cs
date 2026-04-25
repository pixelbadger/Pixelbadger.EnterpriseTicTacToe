using FluentValidation;
using Mediator;
using Pixelbadger.EnterpriseTicTacToe.Application.Common.Mapping;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.SetPresence;

public sealed record SetPresenceCommand(string SessionCode, bool IsOnline, string ClientIdentity) : ICommand<GameStateDto>;

public sealed class SetPresenceCommandValidator : AbstractValidator<SetPresenceCommand>
{
    public SetPresenceCommandValidator()
    {
        RuleFor(request => request.SessionCode)
            .NotEmpty()
            .Length(GameSession.SessionCodeLength)
            .Must(GameStateMapper.IsValidCode);

        RuleFor(request => request.ClientIdentity)
            .NotEmpty();
    }
}

public sealed class SetPresenceCommandHandler(
    IGameSessionRepository gameSessionRepository,
    IGameSessionCache gameSessionCache,
    IGameSessionLock gameSessionLock,
    IGamePresenceTracker gamePresenceTracker,
    IClientIdentityHasher clientIdentityHasher,
    IClock clock)
    : ICommandHandler<SetPresenceCommand, GameStateDto>
{
    public async ValueTask<GameStateDto> Handle(SetPresenceCommand command, CancellationToken cancellationToken)
    {
        var normalizedCode = GameStateMapper.NormalizeCode(command.SessionCode);
        await using var sessionLease = await gameSessionLock.AcquireAsync(normalizedCode, cancellationToken);

        if (!gameSessionCache.TryGet(normalizedCode, out var session))
        {
            session = await gameSessionRepository.GetByCode(normalizedCode, cancellationToken)
                ?? throw new NotFoundException("Game session was not found.");
            gameSessionCache.Set(session);
        }

        var now = clock.UtcNow;
        if (session.IsExpired(now))
        {
            gameSessionCache.Remove(normalizedCode);
            throw new NotFoundException("Game session has expired.");
        }

        var identityHash = clientIdentityHasher.Hash(command.ClientIdentity);
        if (session.FindPlayerByIdentity(identityHash) is null)
        {
            throw new ForbiddenException("Player is not part of this game.");
        }

        gamePresenceTracker.SetPresence(normalizedCode, identityHash, command.IsOnline);

        return GameStateMapper.ToDto(
            session,
            identityHash,
            playerIdentityHash => gamePresenceTracker.IsOnline(session.SessionCode, playerIdentityHash));
    }
}
