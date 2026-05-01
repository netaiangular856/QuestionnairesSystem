using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.ActionPlans;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class InitiativeConfiguration : IEntityTypeConfiguration<Initiative>
{
    public void Configure(EntityTypeBuilder<Initiative> builder)
    {
        builder.ToTable("Initiatives");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DescriptionAr).HasMaxLength(4000);
        builder.Property(x => x.DescriptionEn).HasMaxLength(4000);

        builder.HasIndex(x => x.ActionPlanId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.OwnerUserId);

        builder.HasOne(x => x.ActionPlan)
            .WithMany(x => x.Initiatives)
            .HasForeignKey(x => x.ActionPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OwnerUser)
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.ProgressEntries)
            .WithOne(x => x.Initiative)
            .HasForeignKey(x => x.InitiativeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
