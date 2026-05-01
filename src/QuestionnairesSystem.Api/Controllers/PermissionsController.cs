using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Identity.Interfaces;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Policy = PermissionCodes.RoleManage)]
public sealed class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _permissionService.GetAllAsync(cancellationToken);
        return result.ToApiActionResult(this, traceId);
    }
}
