namespace QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.DTOs;

public sealed class CreateActionPlanRequest
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public Guid? SurveyId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
}
