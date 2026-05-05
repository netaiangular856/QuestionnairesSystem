using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Ai.DTOs;
using QuestionnairesSystem.Application.Features.Ai.Interfaces;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Identity;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/ai")]
public sealed class AiController : ControllerBase
{
    private readonly IAiAssistantService _ai;

    public AiController(IAiAssistantService ai) => _ai = ai;

    [HttpPost("translate-rich-text")]
    public async Task<IActionResult> TranslateRichText(
        [FromBody] TranslateRichTextRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanTranslate(User))
            return Forbid();

        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _ai.TranslateRichTextAsync(request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("suggest-recommendation")]
    [Authorize(Policy = PermissionCodes.RecommendationManage)]
    public async Task<IActionResult> SuggestRecommendation(
        [FromBody] AiSuggestFromSurveyRequest? body,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _ai.SuggestRecommendationAsync(body ?? new AiSuggestFromSurveyRequest(), cancellationToken)
            .ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("suggest-action-plan")]
    [Authorize(Policy = PermissionCodes.ActionPlanManage)]
    public async Task<IActionResult> SuggestActionPlan(
        [FromBody] AiSuggestFromSurveyRequest? body,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _ai.SuggestActionPlanAsync(body ?? new AiSuggestFromSurveyRequest(), cancellationToken)
            .ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("analyze-reports")]
    [Authorize(Policy = PermissionCodes.ReportView)]
    public async Task<IActionResult> AnalyzeReports(
        [FromBody] AiAnalyzeReportsRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _ai.AnalyzeReportsAsync(request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    private static bool CanTranslate(System.Security.Claims.ClaimsPrincipal user) =>
        user.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.SurveyManage)
        || user.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.TemplateManage);
}
