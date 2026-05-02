using QuestionnairesSystem.Application.Features.Organizations.Employees.DTOs;
using QuestionnairesSystem.Shared.Results;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Application.Features.Organizations.Employees.Interfaces;

public interface IEmployeeService
{
    Task<Result<EmployeeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<EmployeeListItemDto>>> GetPagedListAsync(EmployeeFilterRequest request, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDto>> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDto>> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
