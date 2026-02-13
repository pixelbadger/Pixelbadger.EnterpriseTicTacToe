using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pixelbadger.EnterpriseTicTacToe.Domain.Entities;

namespace Pixelbadger.EnterpriseTicTacToe.Infrastructure.Data.Configurations;

public sealed class PlayerSeatConfiguration : IEntityTypeConfiguration<PlayerSeat>
{
    public void Configure(EntityTypeBuilder<PlayerSeat> builder)
    {
        builder.ToTable("Players");

        builder.HasKey(player => player.Id);

        builder.Property(player => player.Id)
            .ValueGeneratedNever();

        builder.Property(player => player.Username)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(player => player.NormalizedUsername)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(player => player.ClientIdentityHash)
            .HasMaxLength(96)
            .IsRequired();

        builder.Property(player => player.Mark)
            .HasConversion<string>()
            .HasMaxLength(1)
            .IsRequired();

        builder.Property(player => player.JoinedUtc)
            .IsRequired();

        builder.Property(player => player.LastSeenUtc)
            .IsRequired();

        builder.HasIndex(player => new { player.GameSessionId, player.Mark })
            .IsUnique();

        builder.HasIndex(player => new { player.GameSessionId, player.NormalizedUsername })
            .IsUnique();
    }
}
