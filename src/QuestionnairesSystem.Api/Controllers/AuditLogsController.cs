using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.AuditLogs.DTOs;
using QuestionnairesSystem.Application.Features.AuditLogs.Interfaces;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = PermissionCodes.AuditLogView)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] AuditLogFilterRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _auditLogService.GetPagedAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<PagedResult<AuditLogDto>>.FromSuccess(result.Value!, traceId));
        }

        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{logId:guid}")]
    public async Task<IActionResult> GetById(Guid logId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _auditLogService.GetByIdAsync(logId, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<AuditLogDto>.FromSuccess(result.Value!, traceId));
        }

        return NotFound(ApiResponse<AuditLogDto>.FromFailure(result.Errors, traceId));
    }
}
