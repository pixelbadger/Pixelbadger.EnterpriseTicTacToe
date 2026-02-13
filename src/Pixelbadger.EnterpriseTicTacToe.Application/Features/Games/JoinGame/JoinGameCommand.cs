using FluentValidation;
using Mediator;
using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Application.Common.Mapping;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Application.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.JoinGame;

public sealed record JoinGameCommand(string SessionCode, string Username, string ClientIdentity) : ICommand<GameStateDto>;

public sealed class JoinGameCommandValidator : AbstractValidator<JoinGameCommand>
{
    public JoinGameCommandValidator()
    {
        RuleFor(request => request.SessionCode)
            .NotEmpty()
            .Length(6)
            .Must(GameStateMapper.IsValidCode);

        RuleFor(request => request.Username)
            .NotEmpty()
            .MaximumLength(80)
            .Must(username => !string.IsNullOrWhiteSpace(username));

        RuleFor(request => request.ClientIdentity)
            .NotEmpty();
    }
}

public sealed class JoinGameCommandHandler(
    IGameSessionRepository gameSessionRepository,
    IClientIdentityHasher clientIdentityHasher,
    IClock clock,
    IOptions<GameSessionSettings> settings)
    : ICommandHandler<JoinGameCommand, GameStateDto>
{
    public async ValueTask<GameStateDto> Handle(JoinGameCommand command, CancellationToken cancellationToken)
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

        var username = command.Username.Trim();
        var normalizedUsername = GameStateMapper.NormalizeUsername(command.Username);
        var identityHash = clientIdentityHasher.Hash(command.ClientIdentity);
        var ttlHours = Math.Max(1, settings.Value.InactivityTimeoutHours);
        var expiresAt = GameStateMapper.CalculateExpiry(now, ttlHours);
        var existingPlayer = session.FindPlayerByNormalizedUsername(normalizedUsername);
        var identityPlayer = session.FindPlayerByIdentity(identityHash);

        if (identityPlayer is not null)
        {
            if (identityPlayer.NormalizedUsername != normalizedUsername)
            {
                throw new ForbiddenException("This anonymous identity is already bound to a different username in this game.");
            }

            session.SetPresence(identityHash, isOnline: true, now, expiresAt);
            await gameSessionRepository.SaveChanges(cancellationToken);
            return GameStateMapper.ToDto(session, identityHash);
        }

        if (existingPlayer is not null)
        {
            if (existingPlayer.ClientIdentityHash != identityHash)
            {
                throw new ForbiddenException("That username is already bound to another anonymous identity.");
            }

            session.SetPresence(identityHash, isOnline: true, now, expiresAt);
            await gameSessionRepository.SaveChanges(cancellationToken);
            return GameStateMapper.ToDto(session, identityHash);
        }

        if (session.PlayerCount >= 2)
        {
            throw new ConflictException("Game is already full.");
        }

        try
        {
            session.JoinOpponent(username, normalizedUsername, identityHash, now, expiresAt);
        }
        catch (DomainRuleViolationException exception)
        {
            throw new ConflictException(exception.Message);
        }

        await gameSessionRepository.SaveChanges(cancellationToken);
        return GameStateMapper.ToDto(session, identityHash);
    }
}
