using FluentValidation;
using Mediator;
using Pixelbadger.EnterpriseTicTacToe.Application.Common.Mapping;
using Pixelbadger.EnterpriseTicTacToe.Application.Contracts;
using Pixelbadger.EnterpriseTicTacToe.Domain.Services;

namespace Pixelbadger.EnterpriseTicTacToe.Application.Features.Games.GetGameState;

public sealed record GetGameStateQuery(string SessionCode, string ClientIdentity) : IQuery<Result<GameStateDto>>;

public sealed class GetGameStateQueryValidator : AbstractValidator<GetGameStateQuery>
{
    public GetGameStateQueryValidator()
    {
        RuleFor(request => request.SessionCode)
            .NotEmpty()
            .Length(6)
            .Must(GameStateMapper.IsValidCode);

        RuleFor(request => request.ClientIdentity)
            .NotEmpty();
    }
}

public sealed class GetGameStateQueryHandler(
    IGameSessionRepository gameSessionRepository,
    IClientIdentityHasher clientIdentityHasher,
    IClock clock)
    : IQueryHandler<GetGameStateQuery, Result<GameStateDto>>
{
    public async ValueTask<Result<GameStateDto>> Handle(GetGameStateQuery query, CancellationToken cancellationToken)
    {
        var normalizedCode = GameStateMapper.NormalizeCode(query.SessionCode);
        var session = await gameSessionRepository.GetByCode(normalizedCode, cancellationToken);
        if (session is null)
        {
            return Result.Failure<GameStateDto>(ResultErrorType.NotFound, "Game session was not found.");
        }

        var now = clock.UtcNow;
        if (session.IsExpired(now))
        {
            return Result.Failure<GameStateDto>(ResultErrorType.NotFound, "Game session has expired.");
        }

        var identityHash = clientIdentityHasher.Hash(query.ClientIdentity);
        if (session.FindPlayerByIdentity(identityHash) is null)
        {
            return Result.Failure<GameStateDto>(ResultErrorType.Forbidden, "Anonymous identity is not part of this game.");
        }

        return Result.Success(GameStateMapper.ToDto(session, identityHash));
    }
}
