using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.Surveys;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        builder.ToTable("Surveys");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DescriptionAr).HasMaxLength(4000);
        builder.Property(x => x.DescriptionEn).HasMaxLength(4000);
        builder.Property(x => x.Code).HasMaxLength(64);
        builder.Property(x => x.RejectionReason).HasMaxLength(2000);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.AudienceScope);
        builder.HasIndex(x => x.OwnerUserId);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[Code] IS NOT NULL");

        builder.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Template)
            .WithMany(x => x.Surveys)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Questions)
            .WithOne(x => x.Survey)
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AudienceMembers)
            .WithOne(x => x.Survey)
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Participants)
            .WithOne(x => x.Survey)
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Responses)
            .WithOne(x => x.Survey)
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Recommendations)
            .WithOne(x => x.Survey)
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.ActionPlans)
            .WithOne(x => x.Survey)
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
