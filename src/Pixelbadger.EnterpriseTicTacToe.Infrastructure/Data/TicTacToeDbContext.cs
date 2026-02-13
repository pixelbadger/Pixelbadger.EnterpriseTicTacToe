using Microsoft.EntityFrameworkCore;
using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;

namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure.Data;

public sealed class TicTacToeDbContext(DbContextOptions<TicTacToeDbContext> options) : DbContext(options)
{
    public DbSet<GameSession> GameSessions => Set<GameSession>();

    public DbSet<PlayerSeat> PlayerSeats => Set<PlayerSeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicTacToeDbContext).Assembly);
    }
}
