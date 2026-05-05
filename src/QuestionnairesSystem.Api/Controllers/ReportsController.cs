using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IQuestionnaireReportService _reports;
    private readonly IImpactMeasurementService _impact;

    public ReportsController(IQuestionnaireReportService reports, IImpactMeasurementService impact)
    {
        _reports = reports;
        _impact = impact;
    }

    [HttpGet("survey-analytics/export/pdf")]
    [Authorize(Policy = PermissionCodes.ReportExport)]
    public async Task<IActionResult> ExportCrossSurveyAnalyticsPdf(
        [FromQuery] CrossSurveyAnalyticsFilterRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _reports.ExportCrossSurveyAnalyticsPdfAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToApiActionResult(this, traceId);
        }

        var name = $"survey-analytics-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";
        return File(result.Value!, "application/pdf", name);
    }

    [HttpGet("survey-analytics/export/excel")]
    [Authorize(Policy = PermissionCodes.ReportExport)]
    public async Task<IActionResult> ExportCrossSurveyAnalyticsExcel(
        [FromQuery] CrossSurveyAnalyticsFilterRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _reports.ExportCrossSurveyAnalyticsExcelAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToApiActionResult(this, traceId);
        }

        var name = $"survey-analytics-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
        return File(
            result.Value!,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            name);
    }

    [HttpGet("survey-analytics")]
    [Authorize(Policy = PermissionCodes.ReportView)]
    [ProducesResponseType(typeof(ApiResponse<CrossSurveyAnalyticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CrossSurveyAnalytics(
        [FromQuery] CrossSurveyAnalyticsFilterRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _reports.GetCrossSurveyAnalyticsAsync(request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("impact-measurement")]
    [Authorize(Policy = PermissionCodes.ReportView)]
    [ProducesResponseType(typeof(ApiResponse<ImpactMeasurementOverviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImpactMeasurement(
        [FromQuery] ImpactMeasurementFilterRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _impact.GetAsync(request, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = PermissionCodes.ReportView)]
    public async Task<IActionResult> Dashboard(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        DashboardFilterRequest? filter = null;
        if (fromUtc.HasValue || toUtc.HasValue)
        {
            filter = new DashboardFilterRequest { FromUtc = fromUtc, ToUtc = toUtc };
        }

        var result = await _reports.GetDashboardAsync(filter, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("executive")]
    [Authorize(Policy = PermissionCodes.ReportView)]
    public async Task<IActionResult> Executive(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _reports.GetExecutiveAsync(cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("surveys/{surveyId:guid}")]
    [Authorize(Policy = PermissionCodes.ReportView)]
    public async Task<IActionResult> SurveyReport(Guid surveyId, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _reports.GetSurveyReportAsync(surveyId, cancellationToken).ConfigureAwait(false);
        return result.ToApiActionResult(this, traceId);
    }

    [HttpGet("export/pdf")]
    [Authorize(Policy = PermissionCodes.ReportExport)]
    public async Task<IActionResult> ExportPdf(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _reports.ExportPdfAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToApiActionResult(this, traceId);
        return File(result.Value!, "application/pdf", "report.pdf");
    }

    [HttpGet("export/excel")]
    [Authorize(Policy = PermissionCodes.ReportExport)]
    public async Task<IActionResult> ExportExcel(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _reports.ExportExcelAsync(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return result.ToApiActionResult(this, traceId);
        return File(result.Value!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "report.xlsx");
    }
}
