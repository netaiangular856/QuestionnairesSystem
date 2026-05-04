using QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.PublicPortal.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Questions.DTOs;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.PublicPortal.Interfaces;

public interface IPublicSurveyPortalService
{
    Task<Result<PagedResult<PublicSurveyListItemDto>>> GetCatalogAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<PublicSurveyPageDto>> GetSurveyByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<QuestionDto>>> ListQuestionsByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<Result<ResponseDetailDto>> CreateResponseByCodeAsync(
        string code,
        CreateResponseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ResponseDetailDto>> SubmitResponseByCodeAsync(
        string code,
        Guid responseId,
        CancellationToken cancellationToken = default);
}
