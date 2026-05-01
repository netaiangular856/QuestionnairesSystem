using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Shared.Abstractions;

namespace QuestionnairesSystem.Domain.Common;

public abstract class AuditableDomainEntity : AuditableEntity
{
    public RecordStatus RecordStatus { get; set; } = RecordStatus.Active;

    public DateTime? DeletedOnUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }
}

