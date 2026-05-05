using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.Interfaces;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.Services;

public sealed class ImpactMeasurementService : IImpactMeasurementService
{
    private const string AllSurveysTitleAr = "جميع الاستبيانات";
    private const string AllSurveysTitleEn = "All surveys";
    private const string AllExecutionTitleAr = "تنفيذ المبادرات (جميع الخطط)";
    private const string AllExecutionTitleEn = "Initiative execution (all plans)";

    private readonly QuestionnairesDbContext _db;

    public ImpactMeasurementService(QuestionnairesDbContext db) => _db = db;

    public async Task<Result<ImpactMeasurementOverviewDto>> GetAsync(
        ImpactMeasurementFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var from = request.FromUtc;
        var to = request.ToUtc;
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            (from, to) = (to, from);
        }

        ImpactMeasurementDto? surveyImpact = null;
        if (request.SurveyId is { } surveyId)
        {
            var surveyResult = await GetSurveyImpactAsync(surveyId, from, to, request.SplitAtUtc, cancellationToken)
                .ConfigureAwait(false);
            if (!surveyResult.IsSuccess)
            {
                return Result<ImpactMeasurementOverviewDto>.Fail(surveyResult.Errors, surveyResult.FailureCode);
            }

            surveyImpact = surveyResult.Value;
        }
        else
        {
            surveyImpact = await TryBuildAllSurveysImpactAsync(from, to, request.SplitAtUtc, cancellationToken)
                .ConfigureAwait(false);
        }

        ImpactMeasurementDto? executionImpact = null;
        if (request.InitiativeId is { } initiativeId)
        {
            var iniResult = await GetInitiativeImpactAsync(initiativeId, from, to, request.SplitAtUtc, cancellationToken)
                .ConfigureAwait(false);
            if (!iniResult.IsSuccess)
            {
                return Result<ImpactMeasurementOverviewDto>.Fail(iniResult.Errors, iniResult.FailureCode);
            }

            executionImpact = iniResult.Value;
        }
        else if (request.ActionPlanId is { } planId)
        {
            var planResult = await GetActionPlanImpactAsync(planId, from, to, request.SplitAtUtc, cancellationToken)
                .ConfigureAwait(false);
            if (!planResult.IsSuccess)
            {
                return Result<ImpactMeasurementOverviewDto>.Fail(planResult.Errors, planResult.FailureCode);
            }

            executionImpact = planResult.Value;
        }
        else
        {
            executionImpact = await TryBuildAllProgressImpactAsync(from, to, request.SplitAtUtc, cancellationToken)
                .ConfigureAwait(false);
        }

        if (surveyImpact == null && executionImpact == null)
        {
            return Result<ImpactMeasurementOverviewDto>.Fail(
                "No survey submissions or initiative progress found for the selected filters.",
                QuestionnaireErrors.ImpactMeasurementInsufficientData);
        }

        return Result<ImpactMeasurementOverviewDto>.Ok(new ImpactMeasurementOverviewDto
        {
            SurveyImpact = surveyImpact,
            ExecutionImpact = executionImpact,
        });
    }

    private async Task<ImpactMeasurementDto?> TryBuildAllSurveysImpactAsync(
        DateTime? from,
        DateTime? to,
        DateTime? userSplit,
        CancellationToken cancellationToken)
    {
        var responsesQuery = _db.SurveyResponses.AsNoTracking()
            .Where(r => r.Status == ResponseStatus.Submitted && r.SubmittedAtUtc != null);
        if (from.HasValue)
        {
            responsesQuery = responsesQuery.Where(r => r.SubmittedAtUtc >= from.Value);
        }

        if (to.HasValue)
        {
            responsesQuery = responsesQuery.Where(r => r.SubmittedAtUtc <= to.Value);
        }

        var timestamps = await responsesQuery
            .Select(r => r.SubmittedAtUtc!.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (timestamps.Count == 0)
        {
            return null;
        }

        string splitBasis;
        DateTime splitUtc;
        if (userSplit.HasValue)
        {
            splitUtc = userSplit.Value;
            splitBasis = "user";
        }
        else
        {
            splitUtc = MedianUtc(timestamps);
            splitBasis = "medianTimestamps";
        }

        var responseIdsBefore = await responsesQuery
            .Where(r => r.SubmittedAtUtc < splitUtc)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var responseIdsAfter = await responsesQuery
            .Where(r => r.SubmittedAtUtc >= splitUtc)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ratingsBefore = await LoadAverageRatingForResponsesAsync(responseIdsBefore, cancellationToken).ConfigureAwait(false);
        var ratingsAfter = await LoadAverageRatingForResponsesAsync(responseIdsAfter, cancellationToken).ConfigureAwait(false);

        return new ImpactMeasurementDto
        {
            ScopeKind = "AllSurveys",
            SubjectId = Guid.Empty,
            SubjectTitleAr = AllSurveysTitleAr,
            SubjectTitleEn = AllSurveysTitleEn,
            SplitAtUtc = splitUtc,
            SplitBasis = splitBasis,
            Before = new ImpactMeasurementBucketDto
            {
                SurveySubmissionCount = responseIdsBefore.Count,
                AverageRating = ratingsBefore,
                ProgressEntryCount = 0,
                AverageProgressPercent = null,
            },
            After = new ImpactMeasurementBucketDto
            {
                SurveySubmissionCount = responseIdsAfter.Count,
                AverageRating = ratingsAfter,
                ProgressEntryCount = 0,
                AverageProgressPercent = null,
            },
            InitiativeStatusDistribution = Array.Empty<NamedCountDto>(),
        };
    }

    private async Task<ImpactMeasurementDto?> TryBuildAllProgressImpactAsync(
        DateTime? from,
        DateTime? to,
        DateTime? userSplit,
        CancellationToken cancellationToken)
    {
        var entriesQuery = _db.InitiativeProgressEntries.AsNoTracking();
        if (from.HasValue)
        {
            entriesQuery = entriesQuery.Where(e => e.RecordedAtUtc >= from.Value);
        }

        if (to.HasValue)
        {
            entriesQuery = entriesQuery.Where(e => e.RecordedAtUtc <= to.Value);
        }

        var entryRows = await entriesQuery
            .Select(e => new { e.RecordedAtUtc, e.ProgressPercent })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (entryRows.Count == 0)
        {
            return null;
        }

        string splitBasis;
        DateTime splitUtc;
        if (userSplit.HasValue)
        {
            splitUtc = userSplit.Value;
            splitBasis = "user";
        }
        else
        {
            splitUtc = MedianUtc(entryRows.Select(e => e.RecordedAtUtc).ToList());
            splitBasis = "medianTimestamps";
        }

        var tupleRows = entryRows.Select(e => (e.RecordedAtUtc, e.ProgressPercent)).ToList();
        var beforeEntries = tupleRows.Where(e => e.RecordedAtUtc < splitUtc).ToList();
        var afterEntries = tupleRows.Where(e => e.RecordedAtUtc >= splitUtc).ToList();

        var statusRows = await _db.Initiatives.AsNoTracking()
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var statusDist = statusRows
            .Select(x => new NamedCountDto { Key = x.Status.ToString(), Count = x.Count })
            .ToList();

        return new ImpactMeasurementDto
        {
            ScopeKind = "AllExecution",
            SubjectId = Guid.Empty,
            SubjectTitleAr = AllExecutionTitleAr,
            SubjectTitleEn = AllExecutionTitleEn,
            SplitAtUtc = splitUtc,
            SplitBasis = splitBasis,
            Before = BuildProgressBucket(beforeEntries),
            After = BuildProgressBucket(afterEntries),
            InitiativeStatusDistribution = statusDist,
        };
    }

    private async Task<Result<ImpactMeasurementDto>> GetSurveyImpactAsync(
        Guid surveyId,
        DateTime? from,
        DateTime? to,
        DateTime? userSplit,
        CancellationToken cancellationToken)
    {
        var survey = await _db.Surveys.AsNoTracking()
            .Where(s => s.Id == surveyId)
            .Select(s => new { s.TitleAr, s.TitleEn })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (survey == null)
        {
            return Result<ImpactMeasurementDto>.Fail("Survey was not found.", QuestionnaireErrors.SurveyNotFound);
        }

        var responsesQuery = _db.SurveyResponses.AsNoTracking()
            .Where(r => r.SurveyId == surveyId && r.Status == ResponseStatus.Submitted && r.SubmittedAtUtc != null);
        if (from.HasValue)
        {
            responsesQuery = responsesQuery.Where(r => r.SubmittedAtUtc >= from.Value);
        }

        if (to.HasValue)
        {
            responsesQuery = responsesQuery.Where(r => r.SubmittedAtUtc <= to.Value);
        }

        var timestamps = await responsesQuery
            .Select(r => r.SubmittedAtUtc!.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (timestamps.Count == 0)
        {
            return Result<ImpactMeasurementDto>.Fail(
                "No submitted responses in the selected period.",
                QuestionnaireErrors.ImpactMeasurementInsufficientData);
        }

        string splitBasis;
        DateTime splitUtc;
        if (userSplit.HasValue)
        {
            splitUtc = userSplit.Value;
            splitBasis = "user";
        }
        else
        {
            var planStarts = await _db.ActionPlans.AsNoTracking()
                .Where(p => p.SurveyId == surveyId && p.StartDateUtc != null)
                .Select(p => p.StartDateUtc!.Value)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (planStarts.Count > 0)
            {
                splitUtc = planStarts.Min();
                splitBasis = "planStart";
            }
            else
            {
                splitUtc = MedianUtc(timestamps);
                splitBasis = "medianTimestamps";
            }
        }

        var responseIdsBefore = await responsesQuery
            .Where(r => r.SubmittedAtUtc < splitUtc)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var responseIdsAfter = await responsesQuery
            .Where(r => r.SubmittedAtUtc >= splitUtc)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ratingsBefore = await LoadAverageRatingForResponsesAsync(responseIdsBefore, cancellationToken).ConfigureAwait(false);
        var ratingsAfter = await LoadAverageRatingForResponsesAsync(responseIdsAfter, cancellationToken).ConfigureAwait(false);

        var dto = new ImpactMeasurementDto
        {
            ScopeKind = "Survey",
            SubjectId = surveyId,
            SubjectTitleAr = survey.TitleAr ?? string.Empty,
            SubjectTitleEn = survey.TitleEn ?? string.Empty,
            SplitAtUtc = splitUtc,
            SplitBasis = splitBasis,
            Before = new ImpactMeasurementBucketDto
            {
                SurveySubmissionCount = responseIdsBefore.Count,
                AverageRating = ratingsBefore,
                ProgressEntryCount = 0,
                AverageProgressPercent = null,
            },
            After = new ImpactMeasurementBucketDto
            {
                SurveySubmissionCount = responseIdsAfter.Count,
                AverageRating = ratingsAfter,
                ProgressEntryCount = 0,
                AverageProgressPercent = null,
            },
            InitiativeStatusDistribution = Array.Empty<NamedCountDto>(),
        };

        return Result<ImpactMeasurementDto>.Ok(dto);
    }

    private async Task<Result<ImpactMeasurementDto>> GetActionPlanImpactAsync(
        Guid planId,
        DateTime? from,
        DateTime? to,
        DateTime? userSplit,
        CancellationToken cancellationToken)
    {
        var plan = await _db.ActionPlans.AsNoTracking()
            .Where(p => p.Id == planId)
            .Select(p => new { p.TitleAr, p.TitleEn, p.StartDateUtc })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (plan == null)
        {
            return Result<ImpactMeasurementDto>.Fail("Action plan was not found.", QuestionnaireErrors.ActionPlanNotFound);
        }

        var initiativeIds = await _db.Initiatives.AsNoTracking()
            .Where(i => i.ActionPlanId == planId)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var statusRows = await _db.Initiatives.AsNoTracking()
            .Where(i => i.ActionPlanId == planId)
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var statusDist = statusRows
            .Select(x => new NamedCountDto { Key = x.Status.ToString(), Count = x.Count })
            .ToList();

        var entriesQuery = _db.InitiativeProgressEntries.AsNoTracking()
            .Where(e => initiativeIds.Contains(e.InitiativeId));
        if (from.HasValue)
        {
            entriesQuery = entriesQuery.Where(e => e.RecordedAtUtc >= from.Value);
        }

        if (to.HasValue)
        {
            entriesQuery = entriesQuery.Where(e => e.RecordedAtUtc <= to.Value);
        }

        var entryRows = await entriesQuery
            .Select(e => new { e.RecordedAtUtc, e.ProgressPercent })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entryRows.Count == 0)
        {
            return Result<ImpactMeasurementDto>.Fail(
                "No initiative progress records in the selected period.",
                QuestionnaireErrors.ImpactMeasurementInsufficientData);
        }

        string splitBasis;
        DateTime splitUtc;
        if (userSplit.HasValue)
        {
            splitUtc = userSplit.Value;
            splitBasis = "user";
        }
        else if (plan.StartDateUtc.HasValue)
        {
            splitUtc = plan.StartDateUtc.Value;
            splitBasis = "planStart";
        }
        else
        {
            splitUtc = MedianUtc(entryRows.Select(e => e.RecordedAtUtc).ToList());
            splitBasis = "medianTimestamps";
        }

        var tupleRows = entryRows.Select(e => (e.RecordedAtUtc, e.ProgressPercent)).ToList();
        var beforeEntries = tupleRows.Where(e => e.RecordedAtUtc < splitUtc).ToList();
        var afterEntries = tupleRows.Where(e => e.RecordedAtUtc >= splitUtc).ToList();

        var dto = new ImpactMeasurementDto
        {
            ScopeKind = "ActionPlan",
            SubjectId = planId,
            SubjectTitleAr = plan.TitleAr ?? string.Empty,
            SubjectTitleEn = plan.TitleEn ?? string.Empty,
            SplitAtUtc = splitUtc,
            SplitBasis = splitBasis,
            Before = BuildProgressBucket(beforeEntries),
            After = BuildProgressBucket(afterEntries),
            InitiativeStatusDistribution = statusDist,
        };

        return Result<ImpactMeasurementDto>.Ok(dto);
    }

    private async Task<Result<ImpactMeasurementDto>> GetInitiativeImpactAsync(
        Guid initiativeId,
        DateTime? from,
        DateTime? to,
        DateTime? userSplit,
        CancellationToken cancellationToken)
    {
        var ini = await _db.Initiatives.AsNoTracking()
            .Where(i => i.Id == initiativeId)
            .Select(i => new { i.TitleAr, i.TitleEn, i.Status })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (ini == null)
        {
            return Result<ImpactMeasurementDto>.Fail("Initiative was not found.", QuestionnaireErrors.InitiativeNotFound);
        }

        var entriesQuery = _db.InitiativeProgressEntries.AsNoTracking()
            .Where(e => e.InitiativeId == initiativeId);
        if (from.HasValue)
        {
            entriesQuery = entriesQuery.Where(e => e.RecordedAtUtc >= from.Value);
        }

        if (to.HasValue)
        {
            entriesQuery = entriesQuery.Where(e => e.RecordedAtUtc <= to.Value);
        }

        var entryRows = await entriesQuery
            .Select(e => new { e.RecordedAtUtc, e.ProgressPercent })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entryRows.Count == 0)
        {
            return Result<ImpactMeasurementDto>.Fail(
                "No progress records in the selected period.",
                QuestionnaireErrors.ImpactMeasurementInsufficientData);
        }

        var planStart = await _db.Initiatives.AsNoTracking()
            .Where(i => i.Id == initiativeId)
            .Join(_db.ActionPlans.AsNoTracking(), i => i.ActionPlanId, p => p.Id, (_, p) => p)
            .Select(p => p.StartDateUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        string splitBasis;
        DateTime splitUtc;
        if (userSplit.HasValue)
        {
            splitUtc = userSplit.Value;
            splitBasis = "user";
        }
        else if (planStart.HasValue)
        {
            splitUtc = planStart.Value;
            splitBasis = "planStart";
        }
        else
        {
            splitUtc = MedianUtc(entryRows.Select(e => e.RecordedAtUtc).ToList());
            splitBasis = "medianTimestamps";
        }

        var tupleRowsIni = entryRows.Select(e => (e.RecordedAtUtc, e.ProgressPercent)).ToList();
        var beforeEntries = tupleRowsIni.Where(e => e.RecordedAtUtc < splitUtc).ToList();
        var afterEntries = tupleRowsIni.Where(e => e.RecordedAtUtc >= splitUtc).ToList();

        var dto = new ImpactMeasurementDto
        {
            ScopeKind = "Initiative",
            SubjectId = initiativeId,
            SubjectTitleAr = ini.TitleAr ?? string.Empty,
            SubjectTitleEn = ini.TitleEn ?? string.Empty,
            SplitAtUtc = splitUtc,
            SplitBasis = splitBasis,
            Before = BuildProgressBucket(beforeEntries),
            After = BuildProgressBucket(afterEntries),
            InitiativeStatusDistribution =
            [
                new NamedCountDto { Key = ini.Status.ToString(), Count = 1 },
            ],
        };

        return Result<ImpactMeasurementDto>.Ok(dto);
    }

    private async Task<double?> LoadAverageRatingForResponsesAsync(
        IReadOnlyCollection<Guid> responseIds,
        CancellationToken cancellationToken)
    {
        if (responseIds.Count == 0)
        {
            return null;
        }

        var jsonRows = await (
                from a in _db.QuestionAnswers.AsNoTracking()
                join q in _db.Questions.AsNoTracking() on a.QuestionId equals q.Id
                where responseIds.Contains(a.ResponseId) &&
                      (q.Type == QuestionType.Rating || q.Type == QuestionType.Scale)
                select a.ValueJson)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var values = new List<double>();
        foreach (var json in jsonRows)
        {
            if (TryParseRatingJson(json, out var v))
            {
                values.Add(v);
            }
        }

        if (values.Count == 0)
        {
            return null;
        }

        return Math.Round(values.Average(), 2);
    }

    private static ImpactMeasurementBucketDto BuildProgressBucket(
        List<(DateTime RecordedAtUtc, decimal? ProgressPercent)> entries)
    {
        var percents = entries
            .Where(e => e.ProgressPercent.HasValue)
            .Select(e => (double)e.ProgressPercent!.Value)
            .ToList();
        double? avg = percents.Count > 0 ? Math.Round(percents.Average(), 2) : null;
        return new ImpactMeasurementBucketDto
        {
            SurveySubmissionCount = 0,
            AverageRating = null,
            ProgressEntryCount = entries.Count,
            AverageProgressPercent = avg,
        };
    }

    private static DateTime MedianUtc(IReadOnlyList<DateTime> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var mid = sorted.Count / 2;
        if (sorted.Count % 2 == 1)
        {
            return sorted[mid];
        }

        return sorted[mid - 1];
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
