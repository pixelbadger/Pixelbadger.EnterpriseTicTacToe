using FluentValidation;
using Mediator;
using Microsoft.Extensions.Options;
using Pixelbadger.EnterpriseTicTacToe.Application.Common.Mapping;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Domain.Configuration;
using Pixelbadger.EnterpriseTicTacToe.Domain.Exceptions;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.MakeMove;

public sealed record MakeMoveCommand(string SessionCode, int CellIndex, string ClientIdentity) : ICommand<Result<GameStateDto>>;

public sealed class MakeMoveCommandValidator : AbstractValidator<MakeMoveCommand>
{
    public MakeMoveCommandValidator()
    {
        RuleFor(request => request.SessionCode)
            .NotEmpty()
            .Length(6)
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
    IClientIdentityHasher clientIdentityHasher,
    IClock clock,
    IOptions<GameSessionSettings> settings)
    : ICommandHandler<MakeMoveCommand, Result<GameStateDto>>
{
    public async ValueTask<Result<GameStateDto>> Handle(MakeMoveCommand command, CancellationToken cancellationToken)
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
            session.MakeMove(identityHash, command.CellIndex, now, expiresAt);
        }
        catch (DomainRuleViolationException exception)
        {
            return Result.Failure<GameStateDto>(ResultErrorType.Conflict, exception.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(GameStateMapper.ToDto(session, identityHash));
    }
}
