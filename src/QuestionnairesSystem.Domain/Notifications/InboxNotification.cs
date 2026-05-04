using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Identity;

namespace QuestionnairesSystem.Domain.Notifications;

public sealed class InboxNotification : AuditableDomainEntity
{
    public Guid UserId { get; set; }
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public string MessageAr { get; set; } = null!;
    public string MessageEn { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    /// <summary>Optional parent id for deep links (e.g. survey id when <see cref="RelatedEntityType"/> is a response).</summary>
    public Guid? RelatedEntityParentId { get; set; }

    public User User { get; set; } = null!;
}
