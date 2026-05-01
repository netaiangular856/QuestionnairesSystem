using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Domain.Identity;

namespace QuestionnairesSystem.Domain.ActionPlans;

public sealed class Initiative : AuditableDomainEntity
{
    public Guid ActionPlanId { get; set; }
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public InitiativeStatus Status { get; set; } = InitiativeStatus.Planned;
    public Guid? OwnerUserId { get; set; }
    public DateTime? TargetDateUtc { get; set; }

    public ActionPlan ActionPlan { get; set; } = null!;
    public User? OwnerUser { get; set; }
    public ICollection<InitiativeProgress> ProgressEntries { get; set; } = new List<InitiativeProgress>();
}
