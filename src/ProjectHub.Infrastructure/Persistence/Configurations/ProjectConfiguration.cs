using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHub.Domain.Entities;
using ProjectHub.Infrastructure.Identity;

namespace ProjectHub.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", table =>
        {
            table.HasCheckConstraint("CK_Projects_Status", "[Status] BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_Projects_Dates", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
        });

        builder.HasKey(p => p.Id);

        // The Id is created in the domain (Guid.CreateVersion7), so the database must not generate it.
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(Project.NameMaxLength);

        builder.Property(p => p.Description)
            .HasMaxLength(Project.DescriptionMaxLength);

        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.StartDate).IsRequired();

        // Dashboard and filter queries read Status often.
        builder.HasIndex(p => p.Status);

        // Domain stores a plain Guid; the real foreign key to Identity is declared here.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}