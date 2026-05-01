using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Identity.DTOs;
using QuestionnairesSystem.Application.Features.Identity.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = PermissionCodes.RoleManage)]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _roleService.CreateAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { roleId = result.Value!.Id },
                ApiResponse<RoleDto>.FromSuccess(result.Value, traceId));
        }

        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] RoleFilterRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _roleService.GetPagedAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<PagedResult<RoleListItemDto>>.FromSuccess(result.Value!, traceId));
        }

        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{roleId:guid}")]
    public async Task<IActionResult> GetById(Guid roleId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _roleService.GetByIdAsync(roleId, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{roleId:guid}")]
    public async Task<IActionResult> Update(
        Guid roleId,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _roleService.UpdateAsync(roleId, request, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{roleId:guid}/permissions")]
    public async Task<IActionResult> AssignPermissions(
        Guid roleId,
        [FromBody] AssignRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _roleService.AssignPermissionsAsync(roleId, request, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }
}
