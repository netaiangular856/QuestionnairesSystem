using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Interfaces;

public interface ISurveyService
{
    Task<Result<SurveyDetailDto>> CreateAsync(CreateSurveyRequest request, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<SurveyListItemDto>>> GetPagedAsync(SurveyFilterRequest request, CancellationToken cancellationToken = default);
    Task<Result<SurveyDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<SurveyDetailDto>> UpdateAsync(Guid id, UpdateSurveyRequest request, CancellationToken cancellationToken = default);
    Task<Result> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<SurveyDetailDto>> DuplicateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<SurveyDetailDto>> PatchStatusAsync(Guid id, PatchSurveyStatusRequest request, CancellationToken cancellationToken = default);
    Task<Result<SurveyDetailDto>> SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<SurveyDetailDto>> ApproveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<SurveyDetailDto>> RejectAsync(Guid id, RejectSurveyRequest request, CancellationToken cancellationToken = default);
    Task<Result<SurveyDetailDto>> PublishAsync(Guid id, PublishSurveyRequest? publishRequest, CancellationToken cancellationToken = default);
    Task<Result<SurveyDetailDto>> CloseAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<SurveyListItemDto>>> GetPendingApprovalPagedAsync(
        SurveyFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<int>> CloseExpiredPublishedSurveysAsync(CancellationToken cancellationToken = default);

    Task<Result<SurveyAnalyticsDto>> GetAnalyticsAsync(Guid surveyId, CancellationToken cancellationToken = default);
    Task<Result<SurveyAnalyticsSummaryDto>> GetAnalyticsSummaryAsync(Guid surveyId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<QuestionAnalyticsItemDto>>> GetQuestionAnalyticsAsync(
        Guid surveyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<SurveyNumericAnalyticsDto>> GetNumericQuestionAnalyticsAsync(
        Guid surveyId,
        CancellationToken cancellationToken = default);
}
