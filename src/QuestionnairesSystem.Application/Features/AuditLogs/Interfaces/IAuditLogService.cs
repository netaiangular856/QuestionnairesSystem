using QuestionnairesSystem.Application.Features.AuditLogs.DTOs;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.AuditLogs.Interfaces;

public interface IAuditLogService
{
    Task<Result<PagedResult<AuditLogDto>>> GetPagedAsync(AuditLogFilterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuditLogDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
