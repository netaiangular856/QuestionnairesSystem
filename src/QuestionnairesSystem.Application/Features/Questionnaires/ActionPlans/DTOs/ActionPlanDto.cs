using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.DTOs;

public sealed class ActionPlanDto
{
    public Guid Id { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public Guid? SurveyId { get; init; }
    public Guid? OwnerUserId { get; init; }

    public string? OwnerDisplayName { get; init; }

    public ActionPlanStatus Status { get; init; }
    public DateTime? StartDateUtc { get; init; }
    public DateTime? EndDateUtc { get; init; }
}
