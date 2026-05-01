using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.Recommendations;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("Recommendations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DescriptionAr).HasMaxLength(4000);
        builder.Property(x => x.DescriptionEn).HasMaxLength(4000);

        builder.HasIndex(x => x.SurveyId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.AssignedToUserId);

        builder.HasOne(x => x.Survey)
            .WithMany(x => x.Recommendations)
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.AssignedToUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
