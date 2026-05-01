using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/surveys")]
public sealed class SurveysController : ControllerBase
{
    private readonly ISurveyService _surveys;

    public SurveysController(ISurveyService surveys) => _surveys = surveys;

    [HttpPost]
    [Authorize(Policy = PermissionCodes.SurveyManage)]
    [ProducesResponseType(typeof(ApiResponse<SurveyDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateSurveyRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.SurveyView)]
    public async Task<IActionResult> List([FromQuery] SurveyFilterRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.GetPagedAsync(request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{surveyId:guid}")]
    [Authorize(Policy = PermissionCodes.SurveyView)]
    public async Task<IActionResult> GetById(Guid surveyId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.GetByIdAsync(surveyId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPut("{surveyId:guid}")]
    [Authorize(Policy = PermissionCodes.SurveyManage)]
    public async Task<IActionResult> Update(Guid surveyId, [FromBody] UpdateSurveyRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.UpdateAsync(surveyId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpDelete("{surveyId:guid}")]
    [Authorize(Policy = PermissionCodes.SurveyManage)]
    public async Task<IActionResult> Delete(Guid surveyId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.SoftDeleteAsync(surveyId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{surveyId:guid}/duplicate")]
    [Authorize(Policy = PermissionCodes.SurveyManage)]
    public async Task<IActionResult> Duplicate(Guid surveyId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.DuplicateAsync(surveyId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPatch("{surveyId:guid}/status")]
    [Authorize(Policy = PermissionCodes.SurveyManage)]
    public async Task<IActionResult> PatchStatus(Guid surveyId, [FromBody] PatchSurveyStatusRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.PatchStatusAsync(surveyId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{surveyId:guid}/submit-for-approval")]
    [Authorize(Policy = PermissionCodes.SurveyManage)]
    public async Task<IActionResult> SubmitForApproval(Guid surveyId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.SubmitForApprovalAsync(surveyId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{surveyId:guid}/approve")]
    [Authorize(Policy = PermissionCodes.SurveyManage)]
    public async Task<IActionResult> Approve(Guid surveyId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.ApproveAsync(surveyId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{surveyId:guid}/reject")]
    [Authorize(Policy = PermissionCodes.SurveyManage)]
    public async Task<IActionResult> Reject(Guid surveyId, [FromBody] RejectSurveyRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.RejectAsync(surveyId, request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{surveyId:guid}/publish")]
    [Authorize(Policy = PermissionCodes.SurveyManage)]
    public async Task<IActionResult> Publish(Guid surveyId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.PublishAsync(surveyId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpPost("{surveyId:guid}/close")]
    [Authorize(Policy = PermissionCodes.SurveyManage)]
    public async Task<IActionResult> Close(Guid surveyId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.CloseAsync(surveyId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{surveyId:guid}/analytics")]
    [Authorize(Policy = PermissionCodes.ReportView)]
    public async Task<IActionResult> Analytics(Guid surveyId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.GetAnalyticsAsync(surveyId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{surveyId:guid}/analytics/summary")]
    [Authorize(Policy = PermissionCodes.ReportView)]
    public async Task<IActionResult> AnalyticsSummary(Guid surveyId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.GetAnalyticsSummaryAsync(surveyId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("{surveyId:guid}/analytics/questions")]
    [Authorize(Policy = PermissionCodes.ReportView)]
    public async Task<IActionResult> QuestionAnalytics(
        Guid surveyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _surveys.GetQuestionAnalyticsAsync(surveyId, page, pageSize, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }
}
