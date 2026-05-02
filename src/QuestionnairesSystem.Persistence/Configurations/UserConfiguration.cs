using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.Identity;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.NameAr)
            .HasMaxLength(200);

        builder.Property(x => x.NameEn)
            .HasMaxLength(200);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(x => x.AvatarFileName)
            .HasMaxLength(260);

        builder.HasIndex(x => x.UserName).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.EmployeeId).IsUnique();

        builder.HasOne(x => x.Employee)
            .WithOne(x => x.User)
            .HasForeignKey<User>(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
