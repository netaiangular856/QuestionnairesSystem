using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Organizations.Employees.DTOs;
using QuestionnairesSystem.Application.Features.Organizations.Employees.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet("{id:guid}")]
    [Authorize(PermissionCodes.EmployeeView)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _employeeService.GetByIdAsync(id, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<EmployeeDto>.FromSuccess(result.Value!, traceId));
        }
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet]
    [Authorize(PermissionCodes.EmployeeView)]
    public async Task<IActionResult> GetPagedList([FromQuery] EmployeeFilterRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _employeeService.GetPagedListAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<PagedResult<EmployeeListItemDto>>.FromSuccess(result.Value!, traceId));
        }
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost]
    [Authorize(PermissionCodes.EmployeeManage)]
    public async Task<IActionResult> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _employeeService.CreateAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, 
                ApiResponse<EmployeeDto>.FromSuccess(result.Value, traceId));
        }
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{id:guid}")]
    [Authorize(PermissionCodes.EmployeeManage)]
    public async Task<IActionResult> Update(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _employeeService.UpdateAsync(id, request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<EmployeeDto>.FromSuccess(result.Value!, traceId));
        }
        return result.ToApiActionResult(this, traceId);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(PermissionCodes.EmployeeManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _employeeService.DeleteAsync(id, cancellationToken);
        if (result.IsSuccess)
        {
            return NoContent();
        }
        return result.ToApiActionResult(this, traceId);
    }
}
