namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;

public sealed class DashboardReportDto
{
    public int TotalSurveys { get; init; }
    public int PublishedSurveys { get; init; }
    public int TotalResponses { get; init; }
    public int OpenActionPlans { get; init; }
}
