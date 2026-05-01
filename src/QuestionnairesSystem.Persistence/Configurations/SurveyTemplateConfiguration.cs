using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.Templates;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class SurveyTemplateConfiguration : IEntityTypeConfiguration<SurveyTemplate>
{
    public void Configure(EntityTypeBuilder<SurveyTemplate> builder)
    {
        builder.ToTable("SurveyTemplates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NameAr).IsRequired().HasMaxLength(300);
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(300);
        builder.Property(x => x.DescriptionAr).HasMaxLength(2000);
        builder.Property(x => x.DescriptionEn).HasMaxLength(2000);
        builder.Property(x => x.StructureJson).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.IsArchived);
    }
}
