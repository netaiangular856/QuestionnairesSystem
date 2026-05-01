namespace QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.DTOs;

public sealed class CreateRecommendationRequest
{
    public Guid? SurveyId { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public int Priority { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime? DueDateUtc { get; set; }
}
