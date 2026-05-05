using QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.Interfaces;

public interface IImpactMeasurementService
{
    Task<Result<ImpactMeasurementOverviewDto>> GetAsync(
        ImpactMeasurementFilterRequest request,
        CancellationToken cancellationToken = default);
}
