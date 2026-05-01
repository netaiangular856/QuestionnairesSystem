using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Participation.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/surveys/{surveyId:guid}/responses")]
public sealed class SurveyResponsesController : ControllerBase
{
    private readonly IParticipantResponseService _svc;

    public SurveyResponsesController(IParticipantResponseService svc) => _svc = svc;

    [HttpPost]
    [Authorize(Policy = PermissionCodes.ResponseManage)]
    public async Task<IActionResult> Create(Guid surveyId, [FromBody] CreateResponseRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.CreateResponseAsync(surveyId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.ResponseView)]
    public async Task<IActionResult> List(
        Guid surveyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.ListResponsesAsync(surveyId, page, pageSize, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }
}
