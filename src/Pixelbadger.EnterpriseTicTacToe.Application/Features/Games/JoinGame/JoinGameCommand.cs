using FluentValidation;
using Mediator;
using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Application.Common.Mapping;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.JoinGame;

public sealed record JoinGameCommand(string SessionCode, string Username, string ClientIdentity) : ICommand<Result<GameStateDto>>;

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
    IUnitOfWork unitOfWork,
    IClientIdentityHasher clientIdentityHasher,
    IClock clock,
    IOptions<GameSessionSettings> settings)
    : ICommandHandler<JoinGameCommand, Result<GameStateDto>>
{
    public async ValueTask<Result<GameStateDto>> Handle(JoinGameCommand command, CancellationToken cancellationToken)
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
                return Result.Failure<GameStateDto>(
                    ResultErrorType.Forbidden,
                    "This anonymous identity is already bound to a different username in this game.");
            }

            session.SetPresence(identityHash, isOnline: true, now, expiresAt);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(GameStateMapper.ToDto(session, identityHash));
        }

        if (existingPlayer is not null)
        {
            if (existingPlayer.ClientIdentityHash != identityHash)
            {
                return Result.Failure<GameStateDto>(
                    ResultErrorType.Forbidden,
                    "That username is already bound to another anonymous identity.");
            }

            session.SetPresence(identityHash, isOnline: true, now, expiresAt);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(GameStateMapper.ToDto(session, identityHash));
        }

        if (session.PlayerCount >= 2)
        {
            return Result.Failure<GameStateDto>(ResultErrorType.Conflict, "Game is already full.");
        }

        try
        {
            session.JoinOpponent(username, normalizedUsername, identityHash, now, expiresAt);
        }
        catch (DomainRuleViolationException exception)
        {
            return Result.Failure<GameStateDto>(ResultErrorType.Conflict, exception.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(GameStateMapper.ToDto(session, identityHash));
    }
}
