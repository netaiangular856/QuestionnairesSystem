using QuestionnairesSystem.Application.Features.Identity.DTOs;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Identity.Interfaces;

public interface IPermissionService
{
    Task<Result> SeedDefaultPermissionsAsync(CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PermissionDto>>> GetAllAsync(CancellationToken cancellationToken = default);
}

