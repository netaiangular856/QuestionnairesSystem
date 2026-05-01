using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.Responses;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class QuestionAnswerConfiguration : IEntityTypeConfiguration<QuestionAnswer>
{
    public void Configure(EntityTypeBuilder<QuestionAnswer> builder)
    {
        builder.ToTable("QuestionAnswers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ValueJson).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.ResponseId, x.QuestionId }).IsUnique();

        builder.HasOne(x => x.Response)
            .WithMany(x => x.Answers)
            .HasForeignKey(x => x.ResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Question)
            .WithMany(x => x.Answers)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
