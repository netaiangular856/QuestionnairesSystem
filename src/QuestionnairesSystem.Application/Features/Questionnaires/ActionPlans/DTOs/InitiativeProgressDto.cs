namespace QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.DTOs;

public sealed class InitiativeProgressDto
{
    public Guid Id { get; init; }
    public Guid InitiativeId { get; init; }
    public decimal? ProgressPercent { get; init; }
    public string? Notes { get; init; }
    public DateTime RecordedAtUtc { get; init; }

    public string? RecordedByDisplayName { get; init; }
}
