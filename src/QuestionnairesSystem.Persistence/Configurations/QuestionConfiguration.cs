using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.Surveys;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.HelpTextAr).HasMaxLength(2000);
        builder.Property(x => x.HelpTextEn).HasMaxLength(2000);
        builder.Property(x => x.OptionsJson).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.SurveyId, x.DisplayOrder });

        builder.HasMany(x => x.Answers)
            .WithOne(x => x.Question)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
