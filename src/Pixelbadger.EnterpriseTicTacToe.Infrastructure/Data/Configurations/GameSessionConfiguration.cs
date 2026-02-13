using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;
using Pixelbadger.EnterpriseTicTacToe.Domain.Enums;

namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure.Data.Configurations;

public sealed class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> builder)
    {
        builder.ToTable("GameSessions");

        builder.HasKey(session => session.Id);

        builder.Property(session => session.SessionCode)
            .HasMaxLength(6)
            .IsRequired();

        builder.HasIndex(session => session.SessionCode)
            .IsUnique();

        builder.Property(session => session.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(session => session.CurrentTurn)
            .HasConversion(
                value => value.HasValue ? value.Value.ToString() : null,
                value => string.IsNullOrWhiteSpace(value) ? null : Enum.Parse<PlayerMark>(value))
            .HasMaxLength(1);

        builder.Property(session => session.Winner)
            .HasConversion(
                value => value.HasValue ? value.Value.ToString() : null,
                value => string.IsNullOrWhiteSpace(value) ? null : Enum.Parse<PlayerMark>(value))
            .HasMaxLength(1);

        builder.Property(session => session.BoardState)
            .HasMaxLength(9)
            .IsFixedLength()
            .IsRequired();

        builder.Property(session => session.CreatedUtc).IsRequired();
        builder.Property(session => session.UpdatedUtc).IsRequired();
        builder.Property(session => session.LastActivityUtc).IsRequired();
        builder.Property(session => session.ExpiresAtUtc).IsRequired();

        builder.Property(session => session.RowVersion)
            .IsRowVersion();

        builder.HasMany(session => session.Players)
            .WithOne(player => player.GameSession)
            .HasForeignKey(player => player.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
