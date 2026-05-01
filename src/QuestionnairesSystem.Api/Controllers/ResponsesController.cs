using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Questionnaires.Participation.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/responses")]
public sealed class ResponsesController : ControllerBase
{
    private readonly IParticipantResponseService _svc;

    public ResponsesController(IParticipantResponseService svc) => _svc = svc;

    [HttpGet("{responseId:guid}")]
    [Authorize(Policy = PermissionCodes.ResponseView)]
    public async Task<IActionResult> Get(Guid responseId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.GetResponseByIdAsync(responseId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{responseId:guid}/submit")]
    [Authorize(Policy = PermissionCodes.ResponseManage)]
    public async Task<IActionResult> Submit(Guid responseId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.SubmitResponseAsync(responseId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }
}
