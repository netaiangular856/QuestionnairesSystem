using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.Organizations;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EmployeeNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.NameAr)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.NameEn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Email)
            .HasMaxLength(320);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(x => x.JobTitleAr)
            .HasMaxLength(200);

        builder.Property(x => x.JobTitleEn)
            .HasMaxLength(200);

        builder.HasIndex(x => x.EmployeeNumber).IsUnique();

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
