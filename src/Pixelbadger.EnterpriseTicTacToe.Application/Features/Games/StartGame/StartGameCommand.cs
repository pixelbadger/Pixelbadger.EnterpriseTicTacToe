using FluentValidation;
using Mediator;
using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Application.Common.Mapping;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.StartGame;

public sealed record StartGameCommand(string Username, string ClientIdentity) : ICommand<Result<GameStateDto>>;

public sealed class StartGameCommandValidator : AbstractValidator<StartGameCommand>
{
    public StartGameCommandValidator()
    {
        RuleFor(request => request.Username)
            .NotEmpty()
            .MaximumLength(80)
            .Must(username => !string.IsNullOrWhiteSpace(username));

        RuleFor(request => request.ClientIdentity)
            .NotEmpty();
    }
}

public sealed class StartGameCommandHandler(
    IGameSessionRepository gameSessionRepository,
    IUnitOfWork unitOfWork,
    ISessionCodeGenerator sessionCodeGenerator,
    IClientIdentityHasher clientIdentityHasher,
    IClock clock,
    IOptions<GameSessionSettings> settings)
    : ICommandHandler<StartGameCommand, Result<GameStateDto>>
{
    public async ValueTask<Result<GameStateDto>> Handle(StartGameCommand command, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var ttlHours = Math.Max(1, settings.Value.InactivityTimeoutHours);
        var expiresAt = GameStateMapper.CalculateExpiry(now, ttlHours);
        var username = command.Username.Trim();
        var normalizedUsername = GameStateMapper.NormalizeUsername(command.Username);
        var identityHash = clientIdentityHasher.Hash(command.ClientIdentity);

        var attempts = 0;
        string code;
        do
        {
            if (attempts++ > 20)
            {
                return Result.Failure<GameStateDto>(ResultErrorType.Conflict, "Unable to allocate a new session code. Please try again.");
            }

            code = sessionCodeGenerator.GenerateCode();
        }
        while (await gameSessionRepository.GetByCode(code, cancellationToken) is not null);

        var session = Domain.Entities.GameSession.Create(
            sessionCode: code,
            username: username,
            normalizedUsername: normalizedUsername,
            clientIdentityHash: identityHash,
            utcNow: now,
            expiresAtUtc: expiresAt);

        await gameSessionRepository.Add(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(GameStateMapper.ToDto(session, identityHash));
    }
}
