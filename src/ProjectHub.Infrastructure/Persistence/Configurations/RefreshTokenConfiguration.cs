using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHub.Domain.Entities;
using ProjectHub.Infrastructure.Identity;

namespace ProjectHub.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(RefreshToken.TokenHashLength);

        builder.Property(t => t.ReplacedByTokenHash)
            .HasMaxLength(RefreshToken.TokenHashLength);

        // Every refresh request looks a token up by its hash.
        builder.HasIndex(t => t.TokenHash).IsUnique();

        // Deleting a user removes their refresh tokens. EF also indexes UserId automatically (it's a foreign key).
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}