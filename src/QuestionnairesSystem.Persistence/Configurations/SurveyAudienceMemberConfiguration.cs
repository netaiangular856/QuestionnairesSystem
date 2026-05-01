using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.Surveys;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class SurveyAudienceMemberConfiguration : IEntityTypeConfiguration<SurveyAudienceMember>
{
    public void Configure(EntityTypeBuilder<SurveyAudienceMember> builder)
    {
        builder.ToTable("SurveyAudienceMembers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(320);

        builder.HasIndex(x => x.SurveyId);
        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => new { x.SurveyId, x.UserId })
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");

        builder.HasIndex(x => new { x.SurveyId, x.Email })
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL");

        builder.HasOne(x => x.Survey)
            .WithMany(x => x.AudienceMembers)
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
