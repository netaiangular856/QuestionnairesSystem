using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Organizations.Departments.DTOs;
using QuestionnairesSystem.Application.Features.Organizations.Departments.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet("{id:guid}")]
    [Authorize(PermissionCodes.DepartmentView)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _departmentService.GetByIdAsync(id, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<DepartmentDto>.FromSuccess(result.Value!, traceId));
        }
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet]
    [Authorize(PermissionCodes.DepartmentView)]
    public async Task<IActionResult> GetPagedList([FromQuery] DepartmentFilterRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _departmentService.GetPagedListAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<PagedResult<DepartmentListItemDto>>.FromSuccess(result.Value!, traceId));
        }
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("tree")]
    [Authorize(PermissionCodes.DepartmentView)]
    public async Task<IActionResult> GetTree(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _departmentService.GetTreeAsync(cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<List<DepartmentTreeNodeDto>>.FromSuccess(result.Value!, traceId));
        }
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost]
    [Authorize(PermissionCodes.DepartmentManage)]
    public async Task<IActionResult> Create(CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _departmentService.CreateAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, 
                ApiResponse<DepartmentDto>.FromSuccess(result.Value, traceId));
        }
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{id:guid}")]
    [Authorize(PermissionCodes.DepartmentManage)]
    public async Task<IActionResult> Update(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _departmentService.UpdateAsync(id, request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<DepartmentDto>.FromSuccess(result.Value!, traceId));
        }
        return result.ToApiActionResult(this, traceId);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(PermissionCodes.DepartmentManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _departmentService.DeleteAsync(id, cancellationToken);
        if (result.IsSuccess)
        {
            return NoContent();
        }
        return result.ToApiActionResult(this, traceId);
    }
}
