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
[Route("api/users")]
[Authorize(Policy = PermissionCodes.UserManage)]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _userService.CreateAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { userId = result.Value!.Id },
                ApiResponse<UserDto>.FromSuccess(result.Value, traceId));
        }

        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] UserFilterRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _userService.GetPagedAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<PagedResult<UserListItemDto>>.FromSuccess(result.Value!, traceId));
        }

        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetById(Guid userId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _userService.GetByIdAsync(userId, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> Update(
        Guid userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _userService.UpdateAsync(userId, request, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPatch("{userId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid userId,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = request.IsActive
            ? await _userService.ActivateAsync(userId, cancellationToken)
            : await _userService.DeactivateAsync(userId, cancellationToken);

        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{userId:guid}/roles")]
    public async Task<IActionResult> AssignRoles(
        Guid userId,
        [FromBody] AssignUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _userService.AssignRolesAsync(userId, request, cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }
}

public sealed class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}
