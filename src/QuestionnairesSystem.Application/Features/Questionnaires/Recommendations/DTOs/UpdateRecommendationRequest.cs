using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.DTOs;

public sealed class UpdateRecommendationRequest
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public int Priority { get; set; }
    public RecommendationStatus Status { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public Guid? SurveyId { get; set; }
}
