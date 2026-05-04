using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestionnairesSystem.Domain.Notifications;

namespace QuestionnairesSystem.Persistence.Configurations;

public sealed class InboxNotificationConfiguration : IEntityTypeConfiguration<InboxNotification>
{
    public void Configure(EntityTypeBuilder<InboxNotification> builder)
    {
        builder.ToTable("InboxNotifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(300);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(300);
        builder.Property(x => x.MessageAr).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.MessageEn).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.RelatedEntityType).HasMaxLength(128);

        builder.HasIndex(x => new { x.UserId, x.IsRead });
        builder.HasIndex(x => x.CreatedOnUtc);

        builder.HasOne(x => x.User)
            .WithMany(x => x.InboxNotifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
