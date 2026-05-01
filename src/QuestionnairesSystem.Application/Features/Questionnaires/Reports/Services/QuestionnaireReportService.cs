using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Interfaces;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.Services;

public sealed class QuestionnaireReportService : IQuestionnaireReportService
{
    private readonly QuestionnairesDbContext _db;
    private readonly ISurveyService _surveys;

    public QuestionnaireReportService(QuestionnairesDbContext db, ISurveyService surveys)
    {
        _db = db;
        _surveys = surveys;
    }

    public async Task<Result<DashboardReportDto>> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var totalSurveys = await _db.Surveys.CountAsync(cancellationToken).ConfigureAwait(false);
        var published = await _db.Surveys.CountAsync(s => s.Status == SurveyStatus.Published, cancellationToken).ConfigureAwait(false);
        var responses = await _db.SurveyResponses.CountAsync(r => r.Status == ResponseStatus.Submitted, cancellationToken)
            .ConfigureAwait(false);
        var openPlans = await _db.ActionPlans.CountAsync(
            p => p.Status == ActionPlanStatus.Draft || p.Status == ActionPlanStatus.Active,
            cancellationToken).ConfigureAwait(false);

        return Result<DashboardReportDto>.Ok(new DashboardReportDto
        {
            TotalSurveys = totalSurveys,
            PublishedSurveys = published,
            TotalResponses = responses,
            OpenActionPlans = openPlans
        });
    }

    public async Task<Result<ExecutiveReportDto>> GetExecutiveAsync(CancellationToken cancellationToken = default)
    {
        var dash = await GetDashboardAsync(cancellationToken).ConfigureAwait(false);
        if (!dash.IsSuccess) return Result<ExecutiveReportDto>.Fail(dash.Errors, dash.FailureCode);

        var recent = await _surveys.GetPagedAsync(
                new SurveyFilterRequest { Page = 1, PageSize = 5 },
                cancellationToken)
            .ConfigureAwait(false);
        if (!recent.IsSuccess)
            return Result<ExecutiveReportDto>.Fail(recent.Errors, recent.FailureCode);

        return Result<ExecutiveReportDto>.Ok(new ExecutiveReportDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Summary = dash.Value!,
            RecentSurveys = recent.Value!.Items
        });
    }

    public async Task<Result<SurveyReportDto>> GetSurveyReportAsync(Guid surveyId, CancellationToken cancellationToken = default)
    {
        var s = await _db.Surveys.AsNoTracking().FirstOrDefaultAsync(x => x.Id == surveyId, cancellationToken).ConfigureAwait(false);
        if (s is null)
            return Result<SurveyReportDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);

        var analytics = await _surveys.GetAnalyticsAsync(surveyId, cancellationToken).ConfigureAwait(false);
        if (!analytics.IsSuccess)
            return Result<SurveyReportDto>.Fail(analytics.Errors, analytics.FailureCode);

        return Result<SurveyReportDto>.Ok(new SurveyReportDto
        {
            SurveyId = surveyId,
            TitleEn = s.TitleEn,
            Analytics = analytics.Value!
        });
    }

    public async Task<Result<byte[]>> ExportPdfAsync(CancellationToken cancellationToken = default)
    {
        var dash = await _db.Surveys.CountAsync(cancellationToken).ConfigureAwait(false);
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(36);
                page.Header().Text("Questionnaires OS — Report").SemiBold().FontSize(18);
                page.Content().PaddingVertical(12).Text($"Total surveys (non-deleted): {dash}").FontSize(11);
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated ");
                    t.Span(DateTime.UtcNow.ToString("u"));
                });
            });
        });
        return Result<byte[]>.Ok(doc.GeneratePdf());
    }

    public async Task<Result<byte[]>> ExportExcelAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Dashboard");
            ws.Cell(1, 1).Value = "Metric";
            ws.Cell(1, 2).Value = "Value";
            var d = await GetDashboardAsync(cancellationToken).ConfigureAwait(false);
            if (!d.IsSuccess)
                return Result<byte[]>.Fail(d.Errors, d.FailureCode);

            var v = d.Value!;
            ws.Cell(2, 1).Value = "Total surveys";
            ws.Cell(2, 2).Value = v.TotalSurveys;
            ws.Cell(3, 1).Value = "Published";
            ws.Cell(3, 2).Value = v.PublishedSurveys;
            ws.Cell(4, 1).Value = "Submitted responses";
            ws.Cell(4, 2).Value = v.TotalResponses;
            ws.Cell(5, 1).Value = "Open action plans";
            ws.Cell(5, 2).Value = v.OpenActionPlans;
            wb.SaveAs(stream);
        }

        return Result<byte[]>.Ok(stream.ToArray());
    }
}
