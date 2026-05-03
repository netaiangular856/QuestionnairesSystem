using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.DTOs;

/// <summary>Row for GET /api/initiatives (all initiatives).</summary>
public sealed class InitiativeListItemDto
{
    public Guid Id { get; init; }
    public Guid ActionPlanId { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public InitiativeStatus Status { get; init; }
    public string? OwnerDisplayName { get; init; }
    public DateTime? TargetDateUtc { get; init; }
    public string ActionPlanTitleAr { get; init; } = string.Empty;
    public string ActionPlanTitleEn { get; init; } = string.Empty;
}
