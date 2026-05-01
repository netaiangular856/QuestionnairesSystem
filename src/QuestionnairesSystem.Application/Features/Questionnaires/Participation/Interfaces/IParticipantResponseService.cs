using QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Participation.Interfaces;

public interface IParticipantResponseService
{
    Task<Result<ParticipantDto>> AddParticipantAsync(Guid surveyId, CreateParticipantRequest request, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ParticipantDto>>> ListParticipantsAsync(
        Guid surveyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Result<ResponseDetailDto>> CreateResponseAsync(Guid surveyId, CreateResponseRequest request, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ResponseListItemDto>>> ListResponsesAsync(
        Guid surveyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<ParticipantDetailDto>> GetParticipantDetailAsync(
        Guid surveyId,
        Guid participantId,
        CancellationToken cancellationToken = default);
    Task<Result<ResponseDetailDto>> GetResponseByIdAsync(Guid responseId, CancellationToken cancellationToken = default);
    Task<Result<ResponseDetailDto>> SubmitResponseAsync(Guid responseId, CancellationToken cancellationToken = default);
}
