using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.Responses;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class SurveyResponseConfiguration : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        builder.ToTable("SurveyResponses");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.SurveyId);
        builder.HasIndex(x => x.ParticipantId);
        builder.HasIndex(x => x.RespondentUserId);
        builder.HasIndex(x => x.Status);

        /* Analytics filters: Status + SubmittedAtUtc (+ SurveyId for scoped reports) */
        builder.HasIndex(e => new { e.Status, e.SubmittedAtUtc });
        builder.HasIndex(e => new { e.SurveyId, e.Status, e.SubmittedAtUtc });

        builder.HasOne(x => x.Survey)
            .WithMany(x => x.Responses)
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Participant)
            .WithMany(x => x.Responses)
            .HasForeignKey(x => x.ParticipantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.RespondentUser)
            .WithMany()
            .HasForeignKey(x => x.RespondentUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Answers)
            .WithOne(x => x.Response)
            .HasForeignKey(x => x.ResponseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
