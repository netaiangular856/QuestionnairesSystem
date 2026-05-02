using QuestionnairesSystem.Application.Features.Organizations.Departments.DTOs;
using QuestionnairesSystem.Shared.Results;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Application.Features.Organizations.Departments.Interfaces;

public interface IDepartmentService
{
    Task<Result<DepartmentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<DepartmentListItemDto>>> GetPagedListAsync(DepartmentFilterRequest request, CancellationToken cancellationToken = default);
    Task<Result<List<DepartmentTreeNodeDto>>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<Result<DepartmentDto>> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<Result<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
