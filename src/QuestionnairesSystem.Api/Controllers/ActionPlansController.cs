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
[Route("api/action-plans")]
public sealed class ActionPlansController : ControllerBase
{
    private readonly IActionPlanCrudService _svc;

    public ActionPlansController(IActionPlanCrudService svc) => _svc = svc;

    [HttpPost]
    [Authorize(Policy = PermissionCodes.ActionPlanManage)]
    public async Task<IActionResult> Create([FromBody] CreateActionPlanRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.ActionPlanView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken cancellationToken = default)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.ListPagedAsync(page, pageSize, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{actionPlanId:guid}")]
    [Authorize(Policy = PermissionCodes.ActionPlanView)]
    public async Task<IActionResult> Get(Guid actionPlanId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.GetByIdAsync(actionPlanId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{actionPlanId:guid}")]
    [Authorize(Policy = PermissionCodes.ActionPlanManage)]
    public async Task<IActionResult> Update(Guid actionPlanId, [FromBody] UpdateActionPlanRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.UpdateAsync(actionPlanId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{actionPlanId:guid}/initiatives")]
    [Authorize(Policy = PermissionCodes.ActionPlanView)]
    public async Task<IActionResult> ListInitiatives(Guid actionPlanId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.ListInitiativesByActionPlanAsync(actionPlanId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{actionPlanId:guid}/initiatives")]
    [Authorize(Policy = PermissionCodes.ActionPlanManage)]
    public async Task<IActionResult> AddInitiative(Guid actionPlanId, [FromBody] CreateInitiativeRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.AddInitiativeAsync(actionPlanId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }
}
