using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestionnairesSystem.Api.Extensions;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.Interfaces;
using QuestionnairesSystem.Shared.Api;

namespace QuestionnairesSystem.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IQuestionnaireReportService _reports;

    public ReportsController(IQuestionnaireReportService reports) => _reports = reports;

    [HttpGet("dashboard")]
    [Authorize(Policy = PermissionCodes.ReportView)]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var result = await _reports.GetDashboardAsync(cancellationToken).ConfigureAwait(false);
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
