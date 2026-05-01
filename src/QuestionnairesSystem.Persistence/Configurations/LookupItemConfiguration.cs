using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.Lookups;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class LookupItemConfiguration : IEntityTypeConfiguration<LookupItem>
{
    public void Configure(EntityTypeBuilder<LookupItem> builder)
    {
        builder.ToTable("LookupItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Category).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(300);
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(300);

        builder.HasIndex(x => new { x.Category, x.Code }).IsUnique();
        builder.HasIndex(x => x.Category);
    }
}
