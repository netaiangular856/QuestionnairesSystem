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

    public async Task<Result<DashboardReportDto>> GetDashboardAsync(
        DashboardFilterRequest? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeDashboardFilter(filter, out var fromUtcEx, out var toExclusiveEx, out var filterError))
            return Result<DashboardReportDto>.Fail(filterError!, QuestionnaireErrors.InvalidOperation);

        var isFiltered = fromUtcEx.HasValue;
        var f = fromUtcEx.GetValueOrDefault();
        var t = toExclusiveEx.GetValueOrDefault();

        var totalSurveys = isFiltered
            ? await _db.Surveys.CountAsync(s => s.CreatedOnUtc >= f && s.CreatedOnUtc < t, cancellationToken).ConfigureAwait(false)
            : await _db.Surveys.CountAsync(cancellationToken).ConfigureAwait(false);
        var published = isFiltered
            ? await _db.Surveys.CountAsync(
                s => s.PublishedAtUtc != null && s.PublishedAtUtc >= f && s.PublishedAtUtc < t,
                cancellationToken).ConfigureAwait(false)
            : await _db.Surveys.CountAsync(s => s.Status == SurveyStatus.Published, cancellationToken).ConfigureAwait(false);
        var responses = isFiltered
            ? await _db.SurveyResponses.CountAsync(
                r => r.Status == ResponseStatus.Submitted && r.SubmittedAtUtc != null && r.SubmittedAtUtc >= f &&
                     r.SubmittedAtUtc < t,
                cancellationToken).ConfigureAwait(false)
            : await _db.SurveyResponses.CountAsync(r => r.Status == ResponseStatus.Submitted, cancellationToken)
                .ConfigureAwait(false);
        var openPlans = isFiltered
            ? await _db.ActionPlans.CountAsync(
                p => (p.Status == ActionPlanStatus.Draft || p.Status == ActionPlanStatus.Active) &&
                     p.CreatedOnUtc >= f && p.CreatedOnUtc < t,
                cancellationToken).ConfigureAwait(false)
            : await _db.ActionPlans.CountAsync(
                p => p.Status == ActionPlanStatus.Draft || p.Status == ActionPlanStatus.Active,
                cancellationToken).ConfigureAwait(false);
        var totalActionPlans = isFiltered
            ? await _db.ActionPlans.CountAsync(p => p.CreatedOnUtc >= f && p.CreatedOnUtc < t, cancellationToken)
                .ConfigureAwait(false)
            : await _db.ActionPlans.CountAsync(cancellationToken).ConfigureAwait(false);

        var totalUsers = isFiltered
            ? await _db.Users.CountAsync(u => u.IsActive && u.CreatedOnUtc >= f && u.CreatedOnUtc < t, cancellationToken)
                .ConfigureAwait(false)
            : await _db.Users.CountAsync(u => u.IsActive, cancellationToken).ConfigureAwait(false);
        var totalInactiveUsers = isFiltered
            ? await _db.Users.CountAsync(u => !u.IsActive && u.CreatedOnUtc >= f && u.CreatedOnUtc < t, cancellationToken)
                .ConfigureAwait(false)
            : await _db.Users.CountAsync(u => !u.IsActive, cancellationToken).ConfigureAwait(false);
        var totalDepartments = isFiltered
            ? await _db.Departments.CountAsync(d => d.CreatedOnUtc >= f && d.CreatedOnUtc < t, cancellationToken)
                .ConfigureAwait(false)
            : await _db.Departments.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalEmployees = isFiltered
            ? await _db.Employees.CountAsync(e => e.CreatedOnUtc >= f && e.CreatedOnUtc < t, cancellationToken)
                .ConfigureAwait(false)
            : await _db.Employees.CountAsync(cancellationToken).ConfigureAwait(false);
        var employeesNoDept = isFiltered
            ? await _db.Employees.CountAsync(
                e => e.DepartmentId == null && e.CreatedOnUtc >= f && e.CreatedOnUtc < t,
                cancellationToken).ConfigureAwait(false)
            : await _db.Employees.CountAsync(e => e.DepartmentId == null, cancellationToken).ConfigureAwait(false);
        var totalPartners = isFiltered
            ? await _db.Partners.CountAsync(p => p.CreatedOnUtc >= f && p.CreatedOnUtc < t, cancellationToken)
                .ConfigureAwait(false)
            : await _db.Partners.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalInitiatives = isFiltered
            ? await _db.Initiatives.CountAsync(i => i.CreatedOnUtc >= f && i.CreatedOnUtc < t, cancellationToken)
                .ConfigureAwait(false)
            : await _db.Initiatives.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalQuestions = isFiltered
            ? await _db.Questions.CountAsync(q => q.CreatedOnUtc >= f && q.CreatedOnUtc < t, cancellationToken)
                .ConfigureAwait(false)
            : await _db.Questions.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalSurveyParticipants = isFiltered
            ? await _db.SurveyParticipants.CountAsync(
                p => (p.InvitedAtUtc ?? p.CreatedOnUtc) >= f && (p.InvitedAtUtc ?? p.CreatedOnUtc) < t,
                cancellationToken).ConfigureAwait(false)
            : await _db.SurveyParticipants.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalRecommendations = isFiltered
            ? await _db.Recommendations.CountAsync(r => r.CreatedOnUtc >= f && r.CreatedOnUtc < t, cancellationToken)
                .ConfigureAwait(false)
            : await _db.Recommendations.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalNotifications = isFiltered
            ? await _db.InboxNotifications.CountAsync(n => n.CreatedOnUtc >= f && n.CreatedOnUtc < t, cancellationToken)
                .ConfigureAwait(false)
            : await _db.InboxNotifications.CountAsync(cancellationToken).ConfigureAwait(false);

        var surveysForStatus = _db.Surveys.AsNoTracking();
        if (isFiltered) surveysForStatus = surveysForStatus.Where(s => s.CreatedOnUtc >= f && s.CreatedOnUtc < t);
        var surveyStatusRows = await surveysForStatus
            .GroupBy(s => s.Status)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var surveyStatusDist = surveyStatusRows
            .Select(x => new NamedCountDto { Key = x.Key.ToString(), Count = x.Count })
            .ToList();

        var initiativesForStatus = _db.Initiatives.AsNoTracking();
        if (isFiltered) initiativesForStatus = initiativesForStatus.Where(i => i.CreatedOnUtc >= f && i.CreatedOnUtc < t);
        var initiativeStatusRows = await initiativesForStatus
            .GroupBy(i => i.Status)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var initiativeStatusDist = initiativeStatusRows
            .Select(x => new NamedCountDto { Key = x.Key.ToString(), Count = x.Count })
            .ToList();

        var plansForStatus = _db.ActionPlans.AsNoTracking();
        if (isFiltered) plansForStatus = plansForStatus.Where(p => p.CreatedOnUtc >= f && p.CreatedOnUtc < t);
        var actionPlanStatusRows = await plansForStatus
            .GroupBy(p => p.Status)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var actionPlanStatusDist = actionPlanStatusRows
            .Select(x => new NamedCountDto { Key = x.Key.ToString(), Count = x.Count })
            .ToList();

        var responsesForStatus = _db.SurveyResponses.AsNoTracking();
        if (isFiltered) responsesForStatus = responsesForStatus.Where(r => r.CreatedOnUtc >= f && r.CreatedOnUtc < t);
        var responseStatusRows = await responsesForStatus
            .GroupBy(r => r.Status)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var responseStatusDist = responseStatusRows
            .Select(x => new NamedCountDto { Key = x.Key.ToString(), Count = x.Count })
            .ToList();

        var surveysForAudience = _db.Surveys.AsNoTracking();
        if (isFiltered) surveysForAudience = surveysForAudience.Where(s => s.CreatedOnUtc >= f && s.CreatedOnUtc < t);
        var audienceRows = await surveysForAudience
            .GroupBy(s => s.AudienceScope)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var audienceDist = audienceRows
            .Select(x => new NamedCountDto { Key = x.Key.ToString(), Count = x.Count })
            .ToList();

        var participantsForStatus = _db.SurveyParticipants.AsNoTracking();
        if (isFiltered)
        {
            participantsForStatus = participantsForStatus.Where(p =>
                (p.InvitedAtUtc ?? p.CreatedOnUtc) >= f && (p.InvitedAtUtc ?? p.CreatedOnUtc) < t);
        }

        var participantRows = await participantsForStatus
            .GroupBy(p => p.Status)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var participantStatusDist = participantRows
            .Select(x => new NamedCountDto { Key = x.Key.ToString(), Count = x.Count })
            .ToList();

        var recommendationsForStatus = _db.Recommendations.AsNoTracking();
        if (isFiltered) recommendationsForStatus = recommendationsForStatus.Where(r => r.CreatedOnUtc >= f && r.CreatedOnUtc < t);
        var recommendationRows = await recommendationsForStatus
            .GroupBy(r => r.Status)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var recommendationStatusDist = recommendationRows
            .Select(x => new NamedCountDto { Key = x.Key.ToString(), Count = x.Count })
            .ToList();

        var questionsForTypes = _db.Questions.AsNoTracking();
        if (isFiltered) questionsForTypes = questionsForTypes.Where(q => q.CreatedOnUtc >= f && q.CreatedOnUtc < t);
        var questionTypeRows = await questionsForTypes
            .GroupBy(q => q.Type)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var questionTypeDist = questionTypeRows
            .Select(x => new NamedCountDto { Key = x.Key.ToString(), Count = x.Count })
            .ToList();

        var partnersForTypes = _db.Partners.AsNoTracking();
        if (isFiltered) partnersForTypes = partnersForTypes.Where(p => p.CreatedOnUtc >= f && p.CreatedOnUtc < t);
        var partnerTypeRows = await partnersForTypes
            .GroupBy(p => p.Type)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var partnerTypeDist = partnerTypeRows
            .Select(x => new NamedCountDto { Key = x.Key.ToString(), Count = x.Count })
            .ToList();

        var fromDay = isFiltered ? f.Date : DateTime.UtcNow.Date.AddDays(-29);
        var toDayExclusive = isFiltered ? t.Date : DateTime.UtcNow.Date.AddDays(1);

        var submissionDays = await _db.SurveyResponses.AsNoTracking()
            .Where(r => r.Status == ResponseStatus.Submitted && r.SubmittedAtUtc != null &&
                        r.SubmittedAtUtc >= fromDay && r.SubmittedAtUtc < toDayExclusive)
            .GroupBy(r => r.SubmittedAtUtc!.Value.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var submissionsTimeline = BuildFilledDailyTimeline(fromDay, toDayExclusive, submissionDays.ToDictionary(x => x.Day, x => x.Count));

        var userRegDays = await _db.Users.AsNoTracking()
            .Where(u => u.CreatedOnUtc >= fromDay && u.CreatedOnUtc < toDayExclusive)
            .GroupBy(u => u.CreatedOnUtc.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var usersTimeline = BuildFilledDailyTimeline(fromDay, toDayExclusive, userRegDays.ToDictionary(x => x.Day, x => x.Count));

        var surveyCreatedDays = await _db.Surveys.AsNoTracking()
            .Where(s => s.CreatedOnUtc >= fromDay && s.CreatedOnUtc < toDayExclusive)
            .GroupBy(s => s.CreatedOnUtc.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var surveysCreatedTimeline = BuildFilledDailyTimeline(fromDay, toDayExclusive, surveyCreatedDays.ToDictionary(x => x.Day, x => x.Count));

        var planCreatedDays = await _db.ActionPlans.AsNoTracking()
            .Where(p => p.CreatedOnUtc >= fromDay && p.CreatedOnUtc < toDayExclusive)
            .GroupBy(p => p.CreatedOnUtc.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var plansCreatedTimeline = BuildFilledDailyTimeline(fromDay, toDayExclusive, planCreatedDays.ToDictionary(x => x.Day, x => x.Count));

        var iniCreatedDays = await _db.Initiatives.AsNoTracking()
            .Where(i => i.CreatedOnUtc >= fromDay && i.CreatedOnUtc < toDayExclusive)
            .GroupBy(i => i.CreatedOnUtc.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var initiativesCreatedTimeline = BuildFilledDailyTimeline(fromDay, toDayExclusive, iniCreatedDays.ToDictionary(x => x.Day, x => x.Count));

        var dowCutoff = DateTime.UtcNow.Date.AddDays(-365);
        var dowQuery = _db.SurveyResponses.AsNoTracking()
            .Where(r => r.Status == ResponseStatus.Submitted && r.SubmittedAtUtc != null);
        dowQuery = isFiltered
            ? dowQuery.Where(r => r.SubmittedAtUtc >= f && r.SubmittedAtUtc < t)
            : dowQuery.Where(r => r.SubmittedAtUtc >= dowCutoff);
        var submittedMoments = await dowQuery
            .Select(r => r.SubmittedAtUtc!.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var dowDict = submittedMoments
            .GroupBy(d => d.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());
        var submissionsByDow = Enum.GetValues<DayOfWeek>()
            .Select(d => new NamedCountDto { Key = d.ToString(), Count = dowDict.TryGetValue(d, out var c) ? c : 0 })
            .ToList();

        var employeesForDept = _db.Employees.AsNoTracking().Where(e => e.DepartmentId != null);
        if (isFiltered) employeesForDept = employeesForDept.Where(e => e.CreatedOnUtc >= f && e.CreatedOnUtc < t);
        var deptRows = await employeesForDept
            .GroupBy(e => e.DepartmentId!.Value)
            .Select(g => new { DeptId = g.Key, Cnt = g.Count() })
            .OrderByDescending(x => x.Cnt)
            .Take(8)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var deptIds = deptRows.Select(x => x.DeptId).ToList();
        IReadOnlyList<DepartmentHeadcountRowDto> topDepts;
        if (deptIds.Count == 0)
        {
            topDepts = Array.Empty<DepartmentHeadcountRowDto>();
        }
        else
        {
            var deptMeta = await _db.Departments.AsNoTracking()
                .Where(d => deptIds.Contains(d.Id))
                .Select(d => new { d.Id, d.NameAr, d.NameEn })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var metaById = deptMeta.ToDictionary(x => x.Id, x => (x.NameAr, x.NameEn));
            topDepts = deptRows
                .Select(r =>
                {
                    var found = metaById.TryGetValue(r.DeptId, out var meta);
                    return new DepartmentHeadcountRowDto
                    {
                        DepartmentId = r.DeptId,
                        TitleAr = found ? meta.NameAr : string.Empty,
                        TitleEn = found ? meta.NameEn : string.Empty,
                        EmployeeCount = r.Cnt,
                    };
                })
                .ToList();
        }

        var topResponses = _db.SurveyResponses.AsNoTracking().Where(r => r.Status == ResponseStatus.Submitted);
        if (isFiltered)
        {
            topResponses = topResponses.Where(r =>
                (r.SubmittedAtUtc ?? r.CreatedOnUtc) >= f && (r.SubmittedAtUtc ?? r.CreatedOnUtc) < t);
        }

        var topRows = await topResponses
            .GroupBy(r => r.SurveyId)
            .Select(g => new { SurveyId = g.Key, Cnt = g.Count() })
            .OrderByDescending(x => x.Cnt)
            .Take(8)
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

        return Result<DashboardReportDto>.Ok(new DashboardReportDto
        {
            TotalSurveys = totalSurveys,
            PublishedSurveys = published,
            TotalResponses = responses,
            OpenActionPlans = openPlans,
            TotalUsers = totalUsers,
            TotalInactiveUsers = totalInactiveUsers,
            TotalDepartments = totalDepartments,
            TotalEmployees = totalEmployees,
            EmployeesWithoutDepartment = employeesNoDept,
            TotalPartners = totalPartners,
            TotalInitiatives = totalInitiatives,
            TotalQuestions = totalQuestions,
            TotalSurveyParticipants = totalSurveyParticipants,
            TotalRecommendations = totalRecommendations,
            TotalActionPlans = totalActionPlans,
            TotalNotifications = totalNotifications,
            SurveyStatusDistribution = surveyStatusDist,
            InitiativeStatusDistribution = initiativeStatusDist,
            ActionPlanStatusDistribution = actionPlanStatusDist,
            ResponseStatusDistribution = responseStatusDist,
            AudienceScopeDistribution = audienceDist,
            ParticipantStatusDistribution = participantStatusDist,
            RecommendationStatusDistribution = recommendationStatusDist,
            QuestionTypeDistribution = questionTypeDist,
            PartnerTypeDistribution = partnerTypeDist,
            SubmissionsByDayOfWeek = submissionsByDow,
            TopDepartmentsByEmployees = topDepts,
            SubmissionsTimelineLast30Days = submissionsTimeline,
            UsersRegisteredTimelineLast30Days = usersTimeline,
            SurveysCreatedTimelineLast30Days = surveysCreatedTimeline,
            ActionPlansCreatedTimelineLast30Days = plansCreatedTimeline,
            InitiativesCreatedTimelineLast30Days = initiativesCreatedTimeline,
            TopSurveysBySubmissions = topSurveys,
            FilterFromUtc = isFiltered ? f : null,
            FilterToUtcExclusive = isFiltered ? t : null,
            GeneratedAtUtc = DateTime.UtcNow,
        });
    }

    private static bool TryNormalizeDashboardFilter(
        DashboardFilterRequest? filter,
        out DateTime? fromUtc,
        out DateTime? toExclusiveUtc,
        out string? error)
    {
        fromUtc = null;
        toExclusiveUtc = null;
        error = null;
        if (filter is null || (!filter.FromUtc.HasValue && !filter.ToUtc.HasValue))
            return true;
        if (!filter.FromUtc.HasValue || !filter.ToUtc.HasValue)
        {
            error = "Dashboard date filter requires both fromUtc and toUtc query parameters.";
            return false;
        }

        var from = EnsureUtc(filter.FromUtc.Value);
        var toEx = EnsureUtc(filter.ToUtc.Value);
        if (from >= toEx)
        {
            error = "fromUtc must be earlier than toUtc.";
            return false;
        }

        if ((toEx - from).TotalDays > 366)
        {
            error = "Date range cannot exceed 366 days.";
            return false;
        }

        fromUtc = from;
        toExclusiveUtc = toEx;
        return true;
    }

    private static DateTime EnsureUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();

    private static List<TimelinePointDto> BuildFilledDailyTimeline(
        DateTime fromDay,
        DateTime toDayExclusive,
        IReadOnlyDictionary<DateTime, int> countsByDay)
    {
        var list = new List<TimelinePointDto>();
        for (var d = fromDay; d < toDayExclusive; d = d.AddDays(1))
        {
            list.Add(new TimelinePointDto
            {
                Date = d.ToString("yyyy-MM-dd"),
                Count = countsByDay.TryGetValue(d, out var c) ? c : 0,
            });
        }

        return list;
    }

    public async Task<Result<ExecutiveReportDto>> GetExecutiveAsync(CancellationToken cancellationToken = default)
    {
        var dash = await GetDashboardAsync(null, cancellationToken).ConfigureAwait(false);
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
            var d = await GetDashboardAsync(null, cancellationToken).ConfigureAwait(false);
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

        var actionPlansQuery = _db.ActionPlans.AsNoTracking();
        if (request.SurveyId.HasValue)
        {
            actionPlansQuery = actionPlansQuery.Where(p => p.SurveyId == request.SurveyId.Value);
        }

        if (from.HasValue)
        {
            actionPlansQuery = actionPlansQuery.Where(p => p.CreatedOnUtc >= from.Value);
        }

        if (to.HasValue)
        {
            actionPlansQuery = actionPlansQuery.Where(p => p.CreatedOnUtc <= to.Value);
        }

        var actionPlansInScope = await actionPlansQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        var apStatusAgg = await actionPlansQuery
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var actionPlanStatusDist = apStatusAgg
            .Select(x => new NamedCountDto { Key = x.Status.ToString(), Count = x.Count })
            .ToList();

        const int maxActionPlanRows = 500;
        var actionPlanRows = await (
                from p in actionPlansQuery
                join s in _db.Surveys.AsNoTracking() on p.SurveyId equals s.Id into sg
                from s in sg.DefaultIfEmpty()
                orderby p.CreatedOnUtc descending
                select new CrossSurveyActionPlanReportRowDto
                {
                    TitleAr = p.TitleAr,
                    TitleEn = p.TitleEn,
                    Status = p.Status.ToString(),
                    LinkedSurveyTitleAr = s != null ? s.TitleAr : null,
                    LinkedSurveyTitleEn = s != null ? s.TitleEn : null,
                    CreatedOnUtc = p.CreatedOnUtc,
                })
            .Take(maxActionPlanRows)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var initiativesFilteredQuery =
            from i in _db.Initiatives.AsNoTracking()
            join p in _db.ActionPlans.AsNoTracking() on i.ActionPlanId equals p.Id
            select new { i, p };

        if (request.SurveyId.HasValue)
        {
            initiativesFilteredQuery = initiativesFilteredQuery.Where(x => x.p.SurveyId == request.SurveyId.Value);
        }

        if (from.HasValue)
        {
            initiativesFilteredQuery = initiativesFilteredQuery.Where(x => x.i.CreatedOnUtc >= from.Value);
        }

        if (to.HasValue)
        {
            initiativesFilteredQuery = initiativesFilteredQuery.Where(x => x.i.CreatedOnUtc <= to.Value);
        }

        var initiativesInScope = await initiativesFilteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        var iniStatusAgg = await initiativesFilteredQuery
            .GroupBy(x => x.i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var initiativeStatusDist = iniStatusAgg
            .Select(x => new NamedCountDto { Key = x.Status.ToString(), Count = x.Count })
            .ToList();

        const int maxInitiativeRows = 500;
        var initiativeRows = await (
                from x in initiativesFilteredQuery
                join s in _db.Surveys.AsNoTracking() on x.p.SurveyId equals s.Id into sg
                from s in sg.DefaultIfEmpty()
                orderby x.i.CreatedOnUtc descending
                select new CrossSurveyInitiativeReportRowDto
                {
                    TitleAr = x.i.TitleAr,
                    TitleEn = x.i.TitleEn,
                    Status = x.i.Status.ToString(),
                    ActionPlanTitleAr = x.p.TitleAr,
                    ActionPlanTitleEn = x.p.TitleEn,
                    LinkedSurveyTitleAr = s != null ? s.TitleAr : null,
                    LinkedSurveyTitleEn = s != null ? s.TitleEn : null,
                    TargetDateUtc = x.i.TargetDateUtc,
                    CreatedOnUtc = x.i.CreatedOnUtc,
                })
            .Take(maxInitiativeRows)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

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
                ActionPlansInScope = actionPlansInScope,
                InitiativesInScope = initiativesInScope,
            },
            SurveyStatusDistribution = surveyStatusDist,
            ResponseStatusDistribution = responseStatusDist,
            AudienceScopeDistribution = audienceDist,
            ParticipantStatusDistribution = participantStatusDist,
            SubmissionsByDayOfWeek = submissionsByDow,
            SubmissionsByDay = timeline,
            TopSurveysBySubmissions = topSurveys,
            ActionPlanStatusDistribution = actionPlanStatusDist,
            InitiativeStatusDistribution = initiativeStatusDist,
            ActionPlans = actionPlanRows,
            Initiatives = initiativeRows,
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
