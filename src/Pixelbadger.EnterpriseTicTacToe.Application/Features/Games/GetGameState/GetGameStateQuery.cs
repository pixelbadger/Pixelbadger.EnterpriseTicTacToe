using FluentValidation;
using Mediator;
using Pixelbadger.EnterpriseTicTacToe.Application.Common.Mapping;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.GetGameState;

public sealed record GetGameStateQuery(string SessionCode, string ClientIdentity) : IQuery<GameStateDto>;

public sealed class GetGameStateQueryValidator : AbstractValidator<GetGameStateQuery>
{
    public GetGameStateQueryValidator()
    {
        RuleFor(request => request.SessionCode)
            .NotEmpty()
            .Length(GameSession.SessionCodeLength)
            .Must(GameStateMapper.IsValidCode);

        RuleFor(request => request.ClientIdentity)
            .NotEmpty();
    }
}

public sealed class GetGameStateQueryHandler(
    IGameSessionRepository gameSessionRepository,
    IGameSessionCache gameSessionCache,
    IGameSessionLock gameSessionLock,
    IGamePresenceTracker gamePresenceTracker,
    IClientIdentityHasher clientIdentityHasher,
    IClock clock)
    : IQueryHandler<GetGameStateQuery, GameStateDto>
{
    public async ValueTask<GameStateDto> Handle(GetGameStateQuery query, CancellationToken cancellationToken)
    {
        var normalizedCode = GameStateMapper.NormalizeCode(query.SessionCode);
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

        var identityHash = clientIdentityHasher.Hash(query.ClientIdentity);
        if (session.FindPlayerByIdentity(identityHash) is null)
        {
            throw new ForbiddenException("Anonymous identity is not part of this game.");
        }

        return GameStateMapper.ToDto(
            session,
            identityHash,
            playerIdentityHash => gamePresenceTracker.IsOnline(session.SessionCode, playerIdentityHash));
    }
}
