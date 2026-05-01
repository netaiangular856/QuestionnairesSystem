using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;

public sealed class ExecutiveReportDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public DashboardReportDto Summary { get; init; } = new();
    public IReadOnlyList<SurveyListItemDto> RecentSurveys { get; init; } = Array.Empty<SurveyListItemDto>();
}
