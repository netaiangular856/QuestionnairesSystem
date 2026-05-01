using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.Audit;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).IsRequired().HasMaxLength(128);
        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(256);
        builder.Property(x => x.EntityId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.OldValuesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.NewValuesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);

        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
