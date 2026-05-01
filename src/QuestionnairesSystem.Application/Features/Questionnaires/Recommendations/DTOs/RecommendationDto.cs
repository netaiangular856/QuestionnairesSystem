using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.DTOs;

public sealed class RecommendationDto
{
    public Guid Id { get; init; }
    public Guid? SurveyId { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public int Priority { get; init; }
    public RecommendationStatus Status { get; init; }
    public Guid? AssignedToUserId { get; init; }

    /// <summary>Assignee — for display.</summary>
    public string? AssignedToDisplayName { get; init; }

    public DateTime? DueDateUtc { get; init; }
}
