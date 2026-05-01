using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;

public sealed class SurveyReportDto
{
    public Guid SurveyId { get; init; }
    public string TitleEn { get; init; } = string.Empty;
    public SurveyAnalyticsDto Analytics { get; init; } = new();
}
