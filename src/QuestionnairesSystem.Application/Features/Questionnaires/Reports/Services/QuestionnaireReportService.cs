using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.Export;
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

    public async Task<Result<CrossSurveyAnalyticsDto>> GetCrossSurveyAnalyticsAsync(
        CrossSurveyAnalyticsFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        /* Heavy aggregates: avoid short default SQL timeout + reduce duplicate scans where possible */
        var prevTimeout = _db.Database.GetCommandTimeout();
        _db.Database.SetCommandTimeout(180);

        try
        {
            return await GetCrossSurveyAnalyticsCoreAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _db.Database.SetCommandTimeout(prevTimeout);
        }
    }

    public async Task<Result<byte[]>> ExportCrossSurveyAnalyticsPdfAsync(
        CrossSurveyAnalyticsFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var exportRequest = new CrossSurveyAnalyticsFilterRequest
        {
            SurveyId = request.SurveyId,
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
            Lang = request.Lang,
            IncludeAnswerDetails = true,
        };
        var analytics = await GetCrossSurveyAnalyticsAsync(exportRequest, cancellationToken).ConfigureAwait(false);
        if (!analytics.IsSuccess)
        {
            return Result<byte[]>.Fail(analytics.Errors, analytics.FailureCode);
        }

        var lang = CrossSurveyAnalyticsReportLocalization.NormalizeLang(request.Lang);
        var pdf = CrossSurveyAnalyticsExportWriter.BuildPdf(analytics.Value!, lang);
        return Result<byte[]>.Ok(pdf);
    }

    public async Task<Result<byte[]>> ExportCrossSurveyAnalyticsExcelAsync(
        CrossSurveyAnalyticsFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var exportRequest = new CrossSurveyAnalyticsFilterRequest
        {
            SurveyId = request.SurveyId,
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
            Lang = request.Lang,
            IncludeAnswerDetails = true,
        };
        var analytics = await GetCrossSurveyAnalyticsAsync(exportRequest, cancellationToken).ConfigureAwait(false);
        if (!analytics.IsSuccess)
        {
            return Result<byte[]>.Fail(analytics.Errors, analytics.FailureCode);
        }

        var lang = CrossSurveyAnalyticsReportLocalization.NormalizeLang(request.Lang);
        var xlsx = CrossSurveyAnalyticsExportWriter.BuildExcel(analytics.Value!, lang);
        return Result<byte[]>.Ok(xlsx);
    }

    private async Task<Result<CrossSurveyAnalyticsDto>> GetCrossSurveyAnalyticsCoreAsync(
        CrossSurveyAnalyticsFilterRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SurveyId is { } sid)
        {
            var exists = await _db.Surveys.AsNoTracking().AnyAsync(s => s.Id == sid, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                return Result<CrossSurveyAnalyticsDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);
            }
        }

        string? filterSurveyTitleAr = null;
        string? filterSurveyTitleEn = null;
        if (request.SurveyId is { } sidTitles)
        {
            var titleMeta = await _db.Surveys.AsNoTracking()
                .Where(s => s.Id == sidTitles)
                .Select(s => new { s.TitleAr, s.TitleEn })
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (titleMeta != null)
            {
                filterSurveyTitleAr = titleMeta.TitleAr;
                filterSurveyTitleEn = titleMeta.TitleEn;
            }
        }

        var from = request.FromUtc;
        var to = request.ToUtc;
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            (from, to) = (to, from);
        }

        var submittedBase = _db.SurveyResponses.AsNoTracking()
            .Where(r => r.Status == ResponseStatus.Submitted && r.SubmittedAtUtc != null);

        if (request.SurveyId.HasValue)
        {
            submittedBase = submittedBase.Where(r => r.SurveyId == request.SurveyId.Value);
        }

        if (from.HasValue)
        {
            submittedBase = submittedBase.Where(r => r.SubmittedAtUtc >= from.Value);
        }

        if (to.HasValue)
        {
            submittedBase = submittedBase.Where(r => r.SubmittedAtUtc <= to.Value);
        }

        var submittedInPeriod = await submittedBase.CountAsync(cancellationToken).ConfigureAwait(false);

        var timelineRows = await submittedBase
            .GroupBy(r => r.SubmittedAtUtc!.Value.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .OrderBy(x => x.Day)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var timeline = timelineRows
            .Select(x => new TimelinePointDto
            {
                Date = x.Day.ToString("yyyy-MM-dd"),
                Count = x.Count,
            })
            .ToList();

        var surveysWithActivity = await submittedBase.Select(r => r.SurveyId).Distinct().CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var topRows = await submittedBase
            .GroupBy(r => r.SurveyId)
            .Select(g => new { SurveyId = g.Key, Cnt = g.Count() })
            .OrderByDescending(x => x.Cnt)
            .Take(10)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var topIds = topRows.Select(x => x.SurveyId).ToList();
        Dictionary<Guid, (string TitleAr, string TitleEn, SurveyStatus Status)> surveyMeta;
        if (topIds.Count == 0)
        {
            surveyMeta = new Dictionary<Guid, (string, string, SurveyStatus)>();
        }
        else
        {
            var metaRows = await _db.Surveys.AsNoTracking()
                .Where(s => topIds.Contains(s.Id))
                .Select(s => new { s.Id, s.TitleAr, s.TitleEn, s.Status })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            surveyMeta = metaRows.ToDictionary(x => x.Id, x => (x.TitleAr, x.TitleEn, x.Status));
        }

        var topSurveys = topRows
            .Select(r =>
            {
                var found = surveyMeta.TryGetValue(r.SurveyId, out var meta);
                return new TopSurveyRowDto
                {
                    SurveyId = r.SurveyId,
                    TitleAr = found ? meta.TitleAr ?? string.Empty : string.Empty,
                    TitleEn = found ? meta.TitleEn ?? string.Empty : string.Empty,
                    Status = found ? meta.Status.ToString() : string.Empty,
                    SubmissionsInPeriod = r.Cnt,
                };
            })
            .ToList();

        var surveysScoped = _db.Surveys.AsNoTracking();
        if (request.SurveyId.HasValue)
        {
            surveysScoped = surveysScoped.Where(s => s.Id == request.SurveyId.Value);
        }

        var surveysInScope = await surveysScoped.CountAsync(cancellationToken).ConfigureAwait(false);
        var publishedSurveys = await surveysScoped.CountAsync(s => s.Status == SurveyStatus.Published, cancellationToken)
            .ConfigureAwait(false);

        var surveyStatusRows = await surveysScoped
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var surveyStatusDist = surveyStatusRows
            .Select(x => new NamedCountDto { Key = x.Status.ToString(), Count = x.Count })
            .ToList();

        var participantsScoped = _db.SurveyParticipants.AsNoTracking();
        if (request.SurveyId.HasValue)
        {
            participantsScoped = participantsScoped.Where(p => p.SurveyId == request.SurveyId.Value);
        }

        var invited = await participantsScoped.CountAsync(cancellationToken).ConfigureAwait(false);

        var responsesScoped = _db.SurveyResponses.AsNoTracking();
        if (request.SurveyId.HasValue)
        {
            responsesScoped = responsesScoped.Where(r => r.SurveyId == request.SurveyId.Value);
        }

        var inProgressOpen = await responsesScoped.CountAsync(r => r.Status == ResponseStatus.InProgress, cancellationToken)
            .ConfigureAwait(false);

        var responseStatusRows = await responsesScoped
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var responseStatusDist = responseStatusRows
            .Select(x => new NamedCountDto { Key = x.Status.ToString(), Count = x.Count })
            .ToList();

        var questionsScoped = _db.Questions.AsNoTracking();
        if (request.SurveyId.HasValue)
        {
            questionsScoped = questionsScoped.Where(q => q.SurveyId == request.SurveyId.Value);
        }

        var questionsTotal = await questionsScoped.CountAsync(cancellationToken).ConfigureAwait(false);

        var audienceRows = await surveysScoped
            .GroupBy(s => s.AudienceScope)
            .Select(g => new { Scope = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var audienceDist = audienceRows
            .Select(x => new NamedCountDto { Key = x.Scope.ToString(), Count = x.Count })
            .ToList();

        var participantStatusRows = await participantsScoped
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var participantStatusDist = participantStatusRows
            .Select(x => new NamedCountDto { Key = x.Status.ToString(), Count = x.Count })
            .ToList();

        var completedParticipants = await participantsScoped
            .CountAsync(p => p.Status == ParticipantStatus.Completed, cancellationToken)
            .ConfigureAwait(false);
        var declinedParticipants = await participantsScoped
            .CountAsync(p => p.Status == ParticipantStatus.Declined, cancellationToken)
            .ConfigureAwait(false);

        var responseTimes = await submittedBase
            .Where(r => r.StartedAtUtc != null)
            .Select(r => new { r.StartedAtUtc, r.SubmittedAtUtc })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        double avgMinutes = 0;
        if (responseTimes.Count > 0)
        {
            avgMinutes = Math.Round(
                responseTimes.Average(x => (x.SubmittedAtUtc!.Value - x.StartedAtUtc!.Value).TotalMinutes),
                1);
        }

        // DayOfWeek in GroupBy is not translatable to SQL — aggregate in memory after projecting dates only.
        var submittedUtcMoments = await submittedBase
            .Select(r => r.SubmittedAtUtc!.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var dowDict = submittedUtcMoments
            .GroupBy(d => d.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());
        var submissionsByDow = Enum.GetValues<DayOfWeek>()
            .Select(d => new NamedCountDto { Key = d.ToString(), Count = dowDict.TryGetValue(d, out var c) ? c : 0 })
            .ToList();

        double perDay;
        if (timeline.Count > 1)
        {
            var spanDays = (timelineRows[^1].Day - timelineRows[0].Day).TotalDays + 1;
            perDay = Math.Round(submittedInPeriod / Math.Max(1, spanDays), 2);
        }
        else if (timeline.Count == 1)
        {
            perDay = submittedInPeriod;
        }
        else if (from.HasValue && to.HasValue)
        {
            var spanDays = (to.Value.Date - from.Value.Date).TotalDays + 1;
            perDay = Math.Round(submittedInPeriod / Math.Max(1, spanDays), 2);
        }
        else
        {
            perDay = submittedInPeriod;
        }

        var ratingJsonRows = await (
                from a in _db.QuestionAnswers.AsNoTracking()
                join r in submittedBase on a.ResponseId equals r.Id
                join q in _db.Questions.AsNoTracking() on a.QuestionId equals q.Id
                where q.Type == QuestionType.Rating || q.Type == QuestionType.Scale
                select a.ValueJson)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var numericRatings = new List<double>();
        foreach (var json in ratingJsonRows)
        {
            if (TryParseRatingJson(json, out var v))
            {
                numericRatings.Add(v);
            }
        }

        IReadOnlyList<RatingAnalyticsDto> ratingsDistribution;
        if (numericRatings.Count == 0)
        {
            ratingsDistribution = Array.Empty<RatingAnalyticsDto>();
        }
        else
        {
            var totalR = numericRatings.Count;
            ratingsDistribution = numericRatings
                .GroupBy(v => (int)v)
                .OrderBy(g => g.Key)
                .Select(g => new RatingAnalyticsDto
                {
                    Rating = g.Key,
                    Count = g.Count(),
                    Percentage = totalR > 0 ? (double)g.Count() / totalR * 100 : 0,
                })
                .ToList();
        }

        var qtypeRows = await (
                from a in _db.QuestionAnswers.AsNoTracking()
                join r in submittedBase on a.ResponseId equals r.Id
                join q in _db.Questions.AsNoTracking() on a.QuestionId equals q.Id
                group a by q.Type into g
                select new { Type = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var questionTypeAnswerTotals = qtypeRows
            .Select(x => new NamedCountDto { Key = x.Type.ToString(), Count = x.Count })
            .ToList();

        const int maxTextAnswersForKeywords = 40_000;
        var textJsonRows = await (
                from a in _db.QuestionAnswers.AsNoTracking()
                join r in submittedBase on a.ResponseId equals r.Id
                join q in _db.Questions.AsNoTracking() on a.QuestionId equals q.Id
                where q.Type == QuestionType.ShortText || q.Type == QuestionType.LongText
                select a.ValueJson)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (textJsonRows.Count > maxTextAnswersForKeywords)
        {
            textJsonRows = textJsonRows.Take(maxTextAnswersForKeywords).ToList();
        }

        var textCorpus = textJsonRows
            .Select(TextAnswerKeywordAggregator.UnwrapJsonAnswerText)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        var textAnswerKeywords = TextAnswerKeywordAggregator.Aggregate(textCorpus);

        IReadOnlyList<CrossSurveyAnswerDetailRowDto> answerDetails = Array.Empty<CrossSurveyAnswerDetailRowDto>();
        if (request.IncludeAnswerDetails)
        {
            var rawAnswerRows = await (
                    from r in submittedBase
                    join s in _db.Surveys.AsNoTracking() on r.SurveyId equals s.Id
                    join a in _db.QuestionAnswers.AsNoTracking() on r.Id equals a.ResponseId
                    join q in _db.Questions.AsNoTracking() on a.QuestionId equals q.Id
                    orderby s.TitleEn, r.SubmittedAtUtc, q.DisplayOrder
                    select new
                    {
                        SurveyTitleAr = s.TitleAr,
                        SurveyTitleEn = s.TitleEn,
                        r.SubmittedAtUtc,
                        RespondentDisplayName = r.RespondentUser == null
                            ? null
                            : (r.RespondentUser.NameAr ?? r.RespondentUser.NameEn ?? r.RespondentUser.UserName),
                        QuestionTitleAr = q.TitleAr,
                        QuestionTitleEn = q.TitleEn,
                        QuestionType = q.Type,
                        q.OptionsJson,
                        a.ValueJson,
                    })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            answerDetails = rawAnswerRows
                .Select(row =>
                {
                    var (ar, en) = CrossSurveyAnswerDetailFormatter.FormatAnswer(
                        row.QuestionType,
                        row.OptionsJson,
                        row.ValueJson);
                    return new CrossSurveyAnswerDetailRowDto
                    {
                        SurveyTitleAr = row.SurveyTitleAr ?? string.Empty,
                        SurveyTitleEn = row.SurveyTitleEn ?? string.Empty,
                        SubmittedAtUtc = row.SubmittedAtUtc,
                        RespondentDisplayName = row.RespondentDisplayName,
                        QuestionTitleAr = row.QuestionTitleAr ?? string.Empty,
                        QuestionTitleEn = row.QuestionTitleEn ?? string.Empty,
                        QuestionTypeKey = row.QuestionType.ToString(),
                        AnswerTextAr = ar,
                        AnswerTextEn = en,
                    };
                })
                .ToList();
        }

        var dto = new CrossSurveyAnalyticsDto
        {
            AppliedFilter = new CrossSurveyAnalyticsFilterSnapshotDto
            {
                SurveyId = request.SurveyId,
                FromUtc = from,
                ToUtc = to,
                SurveyTitleAr = filterSurveyTitleAr,
                SurveyTitleEn = filterSurveyTitleEn,
            },
            Overview = new CrossSurveyOverviewDto
            {
                SurveysInScope = surveysInScope,
                PublishedSurveys = publishedSurveys,
                SubmittedResponsesInPeriod = submittedInPeriod,
                SurveysWithSubmissionsInPeriod = surveysWithActivity,
                InvitedParticipantsInScope = invited,
                InProgressResponsesOpen = inProgressOpen,
                SubmissionsPerDayInPeriod = perDay,
                TotalQuestionsInScope = questionsTotal,
                CompletedParticipantsInScope = completedParticipants,
                DeclinedParticipantsInScope = declinedParticipants,
                AverageMinutesToSubmitInPeriod = avgMinutes,
            },
            SurveyStatusDistribution = surveyStatusDist,
            ResponseStatusDistribution = responseStatusDist,
            AudienceScopeDistribution = audienceDist,
            ParticipantStatusDistribution = participantStatusDist,
            SubmissionsByDayOfWeek = submissionsByDow,
            SubmissionsByDay = timeline,
            TopSurveysBySubmissions = topSurveys,
            RatingsDistribution = ratingsDistribution,
            QuestionTypeAnswerTotals = questionTypeAnswerTotals,
            TextAnswerKeywords = textAnswerKeywords,
            AnswerDetails = answerDetails,
        };

        return Result<CrossSurveyAnalyticsDto>.Ok(dto);
    }

    private static bool TryParseRatingJson(string? raw, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var t = raw.Trim();
        if (double.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        try
        {
            var s = JsonSerializer.Deserialize<string>(t);
            if (!string.IsNullOrWhiteSpace(s) &&
                double.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            value = JsonSerializer.Deserialize<double>(t);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
