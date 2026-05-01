using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/initiatives")]
public sealed class InitiativesController : ControllerBase
{
    private readonly IActionPlanCrudService _svc;

    public InitiativesController(IActionPlanCrudService svc) => _svc = svc;

    [HttpGet("{initiativeId:guid}")]
    [Authorize(Policy = PermissionCodes.ActionPlanView)]
    public async Task<IActionResult> Get(Guid initiativeId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.GetInitiativeAsync(initiativeId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{initiativeId:guid}")]
    [Authorize(Policy = PermissionCodes.ActionPlanManage)]
    public async Task<IActionResult> Update(Guid initiativeId, [FromBody] UpdateInitiativeRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.UpdateInitiativeAsync(initiativeId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{initiativeId:guid}/progress")]
    [Authorize(Policy = PermissionCodes.ActionPlanManage)]
    public async Task<IActionResult> AddProgress(Guid initiativeId, [FromBody] AddInitiativeProgressRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.AddProgressAsync(initiativeId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{initiativeId:guid}/progress")]
    [Authorize(Policy = PermissionCodes.ActionPlanView)]
    public async Task<IActionResult> ListProgress(Guid initiativeId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.ListProgressAsync(initiativeId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }
}
