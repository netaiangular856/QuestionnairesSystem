using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Domain.Identity;

namespace QuestionnairesSystem.Domain.ActionPlans;

public sealed class ActionPlan : AuditableDomainEntity
{
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public Guid? SurveyId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public ActionPlanStatus Status { get; set; } = ActionPlanStatus.Draft;
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }

    public Surveys.Survey? Survey { get; set; }
    public User? OwnerUser { get; set; }
    public ICollection<Initiative> Initiatives { get; set; } = new List<Initiative>();
}
