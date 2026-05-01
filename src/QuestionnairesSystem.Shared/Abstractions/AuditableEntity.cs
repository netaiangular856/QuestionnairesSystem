namespace QuestionnairesSystem.Shared.Abstractions;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedOnUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public Guid? ModifiedByUserId { get; set; }
}

