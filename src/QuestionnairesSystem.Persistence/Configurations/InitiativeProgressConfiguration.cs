using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.ActionPlans;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class InitiativeProgressConfiguration : IEntityTypeConfiguration<InitiativeProgress>
{
    public void Configure(EntityTypeBuilder<InitiativeProgress> builder)
    {
        builder.ToTable("InitiativeProgressEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProgressPercent).HasPrecision(5, 2);
        builder.Property(x => x.Notes).HasMaxLength(4000);

        builder.HasIndex(x => x.InitiativeId);
        builder.HasIndex(x => x.RecordedAtUtc);

        builder.HasOne(x => x.Initiative)
            .WithMany(x => x.ProgressEntries)
            .HasForeignKey(x => x.InitiativeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RecordedByUser)
            .WithMany()
            .HasForeignKey(x => x.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
