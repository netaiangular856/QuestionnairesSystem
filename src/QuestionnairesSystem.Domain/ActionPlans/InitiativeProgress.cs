using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Identity;

namespace QuestionnairesSystem.Domain.ActionPlans;

public sealed class InitiativeProgress : AuditableDomainEntity
{
    public Guid InitiativeId { get; set; }
    public decimal? ProgressPercent { get; set; }
    public string? Notes { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid? RecordedByUserId { get; set; }

    public Initiative Initiative { get; set; } = null!;
    public User? RecordedByUser { get; set; }
}
