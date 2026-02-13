using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;
using Pixelbadger.EnterpriseTicTacToe.Domain.Exceptions;

namespace Pixelbadger.EnterpriseTicTacToe.Domain.Entities;

public sealed class GameSession
{
    private static readonly int[][] WinningLines =
    [
        [0, 1, 2],
        [3, 4, 5],
        [6, 7, 8],
        [0, 3, 6],
        [1, 4, 7],
        [2, 5, 8],
        [0, 4, 8],
        [2, 4, 6]
    ];

    private GameSession()
    {
    }

    public Guid Id { get; private set; }

    public string SessionCode { get; private set; } = string.Empty;

    public GameStatus Status { get; private set; }

    public string BoardState { get; private set; } = ".........";

    public PlayerMark? CurrentTurn { get; private set; }

    public PlayerMark? Winner { get; private set; }

    public bool RematchXReady { get; private set; }

    public bool RematchOReady { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public DateTime UpdatedUtc { get; private set; }

    public DateTime LastActivityUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public List<PlayerSeat> Players { get; private set; } = [];

    public int PlayerCount => Players.Count;

    public static GameSession Create(
        string sessionCode,
        string username,
        string normalizedUsername,
        string clientIdentityHash,
        DateTime utcNow,
        DateTime expiresAtUtc)
    {
        var session = new GameSession
        {
            Id = Guid.NewGuid(),
            SessionCode = sessionCode,
            Status = GameStatus.WaitingForOpponent,
            CurrentTurn = PlayerMark.X,
            Winner = null,
            BoardState = ".........",
            CreatedUtc = utcNow,
            UpdatedUtc = utcNow,
            LastActivityUtc = utcNow,
            ExpiresAtUtc = expiresAtUtc
        };

        session.Players.Add(PlayerSeat.Create(PlayerMark.X, username, normalizedUsername, clientIdentityHash, utcNow));
        return session;
    }

    public PlayerSeat? FindPlayerByIdentity(string clientIdentityHash)
    {
        return Players.SingleOrDefault(player => player.ClientIdentityHash == clientIdentityHash);
    }

    public PlayerSeat? FindPlayerByNormalizedUsername(string normalizedUsername)
    {
        return Players.SingleOrDefault(player => player.NormalizedUsername == normalizedUsername);
    }

    public void JoinOpponent(
        string username,
        string normalizedUsername,
        string clientIdentityHash,
        DateTime utcNow,
        DateTime expiresAtUtc)
    {
        EnsureNotExpired();

        if (PlayerCount >= 2)
        {
            throw new DomainRuleViolationException("Game is already full.");
        }

        if (Players.Any(player => player.NormalizedUsername == normalizedUsername))
        {
            throw new DomainRuleViolationException("Username is already taken in this game.");
        }

        Players.Add(PlayerSeat.Create(PlayerMark.O, username, normalizedUsername, clientIdentityHash, utcNow));
        Status = GameStatus.InProgress;
        CurrentTurn = PlayerMark.X;
        Touch(utcNow, expiresAtUtc);
    }

    public void SetPresence(string clientIdentityHash, bool isOnline, DateTime utcNow, DateTime expiresAtUtc)
    {
        EnsureNotExpired();

        var player = FindPlayerByIdentity(clientIdentityHash)
            ?? throw new DomainRuleViolationException("Player is not part of this game.");

        player.SetPresence(isOnline, utcNow);
        Touch(utcNow, expiresAtUtc);
    }

    public void MakeMove(string clientIdentityHash, int boardIndex, DateTime utcNow, DateTime expiresAtUtc)
    {
        EnsureNotExpired();

        if (Status != GameStatus.InProgress)
        {
            throw new DomainRuleViolationException("Game is not currently in progress.");
        }

        if (boardIndex is < 0 or > 8)
        {
            throw new DomainRuleViolationException("Move must target a board cell from 0 to 8.");
        }

        var player = FindPlayerByIdentity(clientIdentityHash)
            ?? throw new DomainRuleViolationException("Player is not part of this game.");

        if (CurrentTurn != player.Mark)
        {
            throw new DomainRuleViolationException("It is not your turn.");
        }

        if (BoardState[boardIndex] != '.')
        {
            throw new DomainRuleViolationException("The selected board cell is already occupied.");
        }

        var board = BoardState.ToCharArray();
        board[boardIndex] = player.Mark == PlayerMark.X ? 'X' : 'O';
        BoardState = new string(board);

        if (HasWinningLine(board, player.Mark))
        {
            Status = GameStatus.Won;
            Winner = player.Mark;
            CurrentTurn = null;
        }
        else if (board.All(cell => cell != '.'))
        {
            Status = GameStatus.Draw;
            Winner = null;
            CurrentTurn = null;
        }
        else
        {
            CurrentTurn = player.Mark == PlayerMark.X ? PlayerMark.O : PlayerMark.X;
        }

        RematchXReady = false;
        RematchOReady = false;
        Touch(utcNow, expiresAtUtc);
    }

    public bool RegisterRematchVote(string clientIdentityHash, DateTime utcNow, DateTime expiresAtUtc)
    {
        EnsureNotExpired();

        if (Status is not GameStatus.Won and not GameStatus.Draw)
        {
            throw new DomainRuleViolationException("Rematch can only be requested after a completed game.");
        }

        var player = FindPlayerByIdentity(clientIdentityHash)
            ?? throw new DomainRuleViolationException("Player is not part of this game.");

        if (player.Mark == PlayerMark.X)
        {
            RematchXReady = true;
        }
        else
        {
            RematchOReady = true;
        }

        var reset = RematchXReady && RematchOReady;
        if (reset)
        {
            BoardState = ".........";
            Status = GameStatus.InProgress;
            CurrentTurn = PlayerMark.X;
            Winner = null;
            RematchXReady = false;
            RematchOReady = false;
        }

        Touch(utcNow, expiresAtUtc);
        return reset;
    }

    public void MarkExpired(DateTime utcNow)
    {
        Status = GameStatus.Expired;
        CurrentTurn = null;
        Winner = null;
        UpdatedUtc = utcNow;
        LastActivityUtc = utcNow;
    }

    public void Touch(DateTime utcNow, DateTime expiresAtUtc)
    {
        UpdatedUtc = utcNow;
        LastActivityUtc = utcNow;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsExpired(DateTime utcNow)
    {
        return Status == GameStatus.Expired || ExpiresAtUtc <= utcNow;
    }

    private void EnsureNotExpired()
    {
        if (Status == GameStatus.Expired)
        {
            throw new DomainRuleViolationException("Game session has expired.");
        }
    }

    private static bool HasWinningLine(IReadOnlyList<char> board, PlayerMark mark)
    {
        var markToken = mark == PlayerMark.X ? 'X' : 'O';
        return WinningLines.Any(line => board[line[0]] == markToken && board[line[1]] == markToken && board[line[2]] == markToken);
    }
}
