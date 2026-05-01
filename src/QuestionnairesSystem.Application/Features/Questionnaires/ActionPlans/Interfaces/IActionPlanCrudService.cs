using QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.DTOs;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.Interfaces;

public interface IActionPlanCrudService
{
    Task<Result<ActionPlanDto>> CreateAsync(CreateActionPlanRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ActionPlanDto>>> ListAsync(CancellationToken cancellationToken = default);
    Task<Result<ActionPlanDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<ActionPlanDto>> UpdateAsync(Guid id, UpdateActionPlanRequest request, CancellationToken cancellationToken = default);
    Task<Result<InitiativeDto>> AddInitiativeAsync(Guid actionPlanId, CreateInitiativeRequest request, CancellationToken cancellationToken = default);
    Task<Result<InitiativeDto>> GetInitiativeAsync(Guid initiativeId, CancellationToken cancellationToken = default);
    Task<Result<InitiativeDto>> UpdateInitiativeAsync(Guid initiativeId, UpdateInitiativeRequest request, CancellationToken cancellationToken = default);
    Task<Result<InitiativeProgressDto>> AddProgressAsync(Guid initiativeId, AddInitiativeProgressRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InitiativeProgressDto>>> ListProgressAsync(Guid initiativeId, CancellationToken cancellationToken = default);
}
