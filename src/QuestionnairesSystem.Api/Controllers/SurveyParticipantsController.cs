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
[Route("api/surveys/{surveyId:guid}/participants")]
public sealed class SurveyParticipantsController : ControllerBase
{
    private readonly IParticipantResponseService _svc;

    public SurveyParticipantsController(IParticipantResponseService svc) => _svc = svc;

    [HttpPost]
    [Authorize(Policy = PermissionCodes.ParticipantManage)]
    public async Task<IActionResult> Add(Guid surveyId, [FromBody] CreateParticipantRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.AddParticipantAsync(surveyId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{participantId:guid}")]
    [Authorize(Policy = PermissionCodes.ParticipantView)]
    public async Task<IActionResult> GetById(Guid surveyId, Guid participantId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.GetParticipantDetailAsync(surveyId, participantId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.ParticipantView)]
    public async Task<IActionResult> List(
        Guid surveyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _svc.ListParticipantsAsync(surveyId, page, pageSize, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }
}
