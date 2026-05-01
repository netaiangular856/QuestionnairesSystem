using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.Participants;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class SurveyParticipantConfiguration : IEntityTypeConfiguration<SurveyParticipant>
{
    public void Configure(EntityTypeBuilder<SurveyParticipant> builder)
    {
        builder.ToTable("SurveyParticipants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.ExternalReference).HasMaxLength(256);

        builder.HasIndex(x => x.SurveyId);
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.Survey)
            .WithMany(x => x.Participants)
            .HasForeignKey(x => x.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Responses)
            .WithOne(x => x.Participant)
            .HasForeignKey(x => x.ParticipantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
