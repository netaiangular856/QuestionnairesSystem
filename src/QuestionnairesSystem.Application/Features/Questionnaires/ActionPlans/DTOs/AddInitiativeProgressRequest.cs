namespace QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.DTOs;

public sealed class AddInitiativeProgressRequest
{
    public decimal? ProgressPercent { get; set; }
    public string? Notes { get; set; }
}
