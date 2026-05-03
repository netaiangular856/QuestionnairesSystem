using QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.DTOs;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.Interfaces;

public interface IRecommendationCrudService
{
    Task<Result<RecommendationDto>> CreateAsync(CreateRecommendationRequest request, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<RecommendationDto>>> ListPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<RecommendationDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<RecommendationDto>> UpdateAsync(Guid id, UpdateRecommendationRequest request, CancellationToken cancellationToken = default);
    Task<Result> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
