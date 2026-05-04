using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.PublicPortal.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.PublicPortal.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/public/surveys")]
[AllowAnonymous]
public sealed class PublicSurveysController : ControllerBase
{
    private readonly IPublicSurveyPortalService _portal;

    public PublicSurveysController(IPublicSurveyPortalService portal) => _portal = portal;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PublicSurveyListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Catalog([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _portal.GetCatalogAsync(page, pageSize, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _portal.GetSurveyByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{code}/questions")]
    public async Task<IActionResult> Questions(string code, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _portal.ListQuestionsByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{code}/responses")]
    public async Task<IActionResult> CreateResponse(string code, [FromBody] CreateResponseRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _portal.CreateResponseByCodeAsync(code, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{code}/responses/{responseId:guid}/submit")]
    public async Task<IActionResult> Submit(string code, Guid responseId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _portal.SubmitResponseByCodeAsync(code, responseId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }
}
