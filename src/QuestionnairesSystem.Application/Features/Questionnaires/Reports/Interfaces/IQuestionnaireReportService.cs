using QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.Interfaces;

public interface IQuestionnaireReportService
{
    Task<Result<DashboardReportDto>> GetDashboardAsync(
        DashboardFilterRequest? filter = null,
        CancellationToken cancellationToken = default);
    Task<Result<ExecutiveReportDto>> GetExecutiveAsync(CancellationToken cancellationToken = default);
    Task<Result<SurveyReportDto>> GetSurveyReportAsync(Guid surveyId, CancellationToken cancellationToken = default);
    Task<Result<byte[]>> ExportPdfAsync(CancellationToken cancellationToken = default);
    Task<Result<byte[]>> ExportExcelAsync(CancellationToken cancellationToken = default);

    Task<Result<CrossSurveyAnalyticsDto>> GetCrossSurveyAnalyticsAsync(
        CrossSurveyAnalyticsFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<byte[]>> ExportCrossSurveyAnalyticsPdfAsync(
        CrossSurveyAnalyticsFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<byte[]>> ExportCrossSurveyAnalyticsExcelAsync(
        CrossSurveyAnalyticsFilterRequest request,
        CancellationToken cancellationToken = default);
}
