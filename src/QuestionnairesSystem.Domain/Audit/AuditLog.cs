using QuestionnairesSystem.Shared.Abstractions;

namespace QuestionnairesSystem.Domain.Audit;

/// <summary>Immutable audit trail; not soft-deleted.</summary>
public sealed class AuditLog : AuditableEntity
{
    public DateTime OccurredAtUtc { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
