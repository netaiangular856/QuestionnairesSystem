using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.ActionPlans;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class ActionPlanConfiguration : IEntityTypeConfiguration<ActionPlan>
{
    public void Configure(EntityTypeBuilder<ActionPlan> builder)
    {
        builder.ToTable("ActionPlans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DescriptionAr).HasMaxLength(4000);
        builder.Property(x => x.DescriptionEn).HasMaxLength(4000);

        builder.HasIndex(x => x.SurveyId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.OwnerUserId);

        builder.HasOne(x => x.Survey)
            .WithMany(x => x.ActionPlans)
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.OwnerUser)
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Initiatives)
            .WithOne(x => x.ActionPlan)
            .HasForeignKey(x => x.ActionPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
