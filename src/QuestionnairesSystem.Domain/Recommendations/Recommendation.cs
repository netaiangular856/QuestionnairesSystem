using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Domain.Identity;

namespace QuestionnairesSystem.Domain.Recommendations;

public sealed class Recommendation : AuditableDomainEntity
{
    public Guid? SurveyId { get; set; }
    public string TitleAr { get; set; } = null!;
    public string TitleEn { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public int Priority { get; set; }
    public RecommendationStatus Status { get; set; } = RecommendationStatus.Draft;
    public Guid? AssignedToUserId { get; set; }
    public DateTime? DueDateUtc { get; set; }

    public Surveys.Survey? Survey { get; set; }
    public User? AssignedToUser { get; set; }
}
