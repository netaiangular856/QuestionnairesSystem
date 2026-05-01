using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Identity;

namespace QuestionnairesSystem.Domain.Notifications;

public sealed class InboxNotification : AuditableDomainEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }

    public User User { get; set; } = null!;
}
