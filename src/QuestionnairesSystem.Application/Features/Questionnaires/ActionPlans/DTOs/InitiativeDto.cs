using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.DTOs;

public sealed class InitiativeDto
{
    public Guid Id { get; init; }
    public Guid ActionPlanId { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public InitiativeStatus Status { get; init; }
    public Guid? OwnerUserId { get; init; }

    public string? OwnerDisplayName { get; init; }

    public DateTime? TargetDateUtc { get; init; }
}
