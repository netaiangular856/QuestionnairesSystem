using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Ai.DTOs;
using QuestionnairesSystem.Application.Features.Ai.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.Export;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Interfaces;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Ai.Services;

public sealed partial class AiAssistantService : IAiAssistantService
{
    private const int MaxTranslateChars = 120_000;
    private const int MaxSuggestAnswerSampleRows = 120;
    private const int MaxSuggestAnswerExcerptChars = 420;

    private static readonly JsonSerializerOptions AiDeserialize = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly OpenAiChatClient _openAi;
    private readonly IQuestionnaireReportService _reports;
    private readonly QuestionnairesDbContext _db;
    private readonly ISurveyService _surveys;

    public AiAssistantService(
        OpenAiChatClient openAi,
        IQuestionnaireReportService reports,
        QuestionnairesDbContext db,
        ISurveyService surveys)
    {
        _openAi = openAi;
        _reports = reports;
        _db = db;
        _surveys = surveys;
    }

    public async Task<Result<TranslateRichTextResponse>> TranslateRichTextAsync(
        TranslateRichTextRequest request,
        CancellationToken cancellationToken = default)
    {
        var src = (request.SourceLang ?? "ar").Trim().ToLowerInvariant();
        var tgt = (request.TargetLang ?? "en").Trim().ToLowerInvariant();
        if (src is not ("ar" or "en") || tgt is not ("ar" or "en") || src == tgt)
        {
            return Result<TranslateRichTextResponse>.Fail(
                "Invalid source/target language. Use ar and en.",
                QuestionnaireErrors.InvalidOperation);
        }

        var html = request.Html ?? string.Empty;
        if (html.Length > MaxTranslateChars)
        {
            return Result<TranslateRichTextResponse>.Fail(
                $"Text exceeds maximum length ({MaxTranslateChars}).",
                QuestionnaireErrors.InvalidOperation);
        }

        if (string.IsNullOrWhiteSpace(html))
        {
            return Result<TranslateRichTextResponse>.Ok(new TranslateRichTextResponse { Html = string.Empty });
        }

        var system =
            "You translate HTML fragments for a bilingual enterprise app. Preserve ALL tags and attributes; " +
            "translate only human-visible text nodes. Do not add explanations. " +
            "Output valid HTML only. Never wrap the output in markdown code fences (no triple backticks or ```html).";
        var user =
            $"Translate from {(src == "ar" ? "Arabic" : "English")} to {(tgt == "ar" ? "Arabic" : "English")}.\n\n" +
            html;

        var completion = await _openAi.CompleteAsync(
            new List<(string, string)> { ("system", system), ("user", user) },
            jsonObjectFormat: false,
            cancellationToken).ConfigureAwait(false);

        if (!completion.IsSuccess)
            return Result<TranslateRichTextResponse>.Fail(completion.Errors, completion.FailureCode);

        // Model may return plain HTML or JSON wrapper — handle simple JSON { "html": "..." }
        var text = completion.Value!.Trim();
        if (text.StartsWith('{') && text.Contains("\"html\"", StringComparison.Ordinal))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.TryGetProperty("html", out var h))
                    text = h.GetString() ?? text;
            }
            catch
            {
                /* use raw */
            }
        }

        text = StripMarkdownCodeFences(text);

        return Result<TranslateRichTextResponse>.Ok(new TranslateRichTextResponse { Html = text });
    }

    public async Task<Result<AiSuggestRecommendationDraftDto>> SuggestRecommendationAsync(
        AiSuggestFromSurveyRequest request,
        CancellationToken cancellationToken = default)
    {
        var filter = new CrossSurveyAnalyticsFilterRequest
        {
            SurveyId = request.SurveyId,
            IncludeAnswerDetails = false,
        };

        // Cross-survey: default last 30 days so excerpts are bounded (same idea as sentiment).
        if (!filter.SurveyId.HasValue && !filter.FromUtc.HasValue && !filter.ToUtc.HasValue)
        {
            var now = DateTime.UtcNow;
            filter.ToUtc = now;
            filter.FromUtc = now.AddDays(-30);
        }

        var contextResult = await BuildAnswerExcerptEvidenceForAiAsync(
                filter,
                "recommendation_from_answer_excerpts",
                cancellationToken)
            .ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<AiSuggestRecommendationDraftDto>.Fail(contextResult.Errors, contextResult.FailureCode);

        var context = contextResult.Value!.Json;
        var user = AiPromptEngine.BuildRecommendationDraftUserPrompt(context);

        var completion = await _openAi.CompleteAsync(
            new List<(string, string)> { ("system", AiPromptEngine.RecommendationDraftFromAnswersSystem), ("user", user) },
            jsonObjectFormat: true,
            cancellationToken).ConfigureAwait(false);

        if (!completion.IsSuccess)
            return Result<AiSuggestRecommendationDraftDto>.Fail(completion.Errors, completion.FailureCode);

        try
        {
            var draft = JsonSerializer.Deserialize<AiSuggestRecommendationDraftDto>(completion.Value!, AiDeserialize);
            if (draft == null || string.IsNullOrWhiteSpace(draft.TitleAr) || string.IsNullOrWhiteSpace(draft.TitleEn))
            {
                return Result<AiSuggestRecommendationDraftDto>.Fail(
                    "AI returned an incomplete recommendation draft.",
                    QuestionnaireErrors.AiProviderError);
            }

            draft.Priority = draft.Priority switch
            {
                3 or 5 or 8 => draft.Priority,
                _ => 5,
            };

            return Result<AiSuggestRecommendationDraftDto>.Ok(draft);
        }
        catch (Exception ex)
        {
            return Result<AiSuggestRecommendationDraftDto>.Fail(
                ex.Message,
                QuestionnaireErrors.AiProviderError);
        }
    }

    public async Task<Result<AiSuggestActionPlanDraftDto>> SuggestActionPlanAsync(
        AiSuggestFromSurveyRequest request,
        CancellationToken cancellationToken = default)
    {
        var filter = new CrossSurveyAnalyticsFilterRequest
        {
            SurveyId = request.SurveyId,
            IncludeAnswerDetails = false,
        };

        if (!filter.SurveyId.HasValue && !filter.FromUtc.HasValue && !filter.ToUtc.HasValue)
        {
            var now = DateTime.UtcNow;
            filter.ToUtc = now;
            filter.FromUtc = now.AddDays(-30);
        }

        var contextResult = await BuildAnswerExcerptEvidenceForAiAsync(
                filter,
                "action_plan_from_answer_excerpts",
                cancellationToken)
            .ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<AiSuggestActionPlanDraftDto>.Fail(contextResult.Errors, contextResult.FailureCode);

        var context = contextResult.Value!.Json;
        var user = AiPromptEngine.BuildActionPlanDraftUserPrompt(context);

        var completion = await _openAi.CompleteAsync(
            new List<(string, string)> { ("system", AiPromptEngine.ActionPlanDraftFromAnswersSystem), ("user", user) },
            jsonObjectFormat: true,
            cancellationToken).ConfigureAwait(false);

        if (!completion.IsSuccess)
            return Result<AiSuggestActionPlanDraftDto>.Fail(completion.Errors, completion.FailureCode);

        try
        {
            var draft = JsonSerializer.Deserialize<AiSuggestActionPlanDraftDto>(completion.Value!, AiDeserialize);
            if (draft == null || string.IsNullOrWhiteSpace(draft.TitleAr) || string.IsNullOrWhiteSpace(draft.TitleEn))
            {
                return Result<AiSuggestActionPlanDraftDto>.Fail(
                    "AI returned an incomplete action plan draft.",
                    QuestionnaireErrors.AiProviderError);
            }

            return Result<AiSuggestActionPlanDraftDto>.Ok(draft);
        }
        catch (Exception ex)
        {
            return Result<AiSuggestActionPlanDraftDto>.Fail(
                ex.Message,
                QuestionnaireErrors.AiProviderError);
        }
    }

    public async Task<Result<AiAnalyzeReportsResponseDto>> AnalyzeReportsAsync(
        AiAnalyzeReportsRequest request,
        CancellationToken cancellationToken = default)
    {
        var analyticsResult = await _reports.GetCrossSurveyAnalyticsAsync(request.Filter, cancellationToken)
            .ConfigureAwait(false);
        if (!analyticsResult.IsSuccess)
            return Result<AiAnalyzeReportsResponseDto>.Fail(analyticsResult.Errors, analyticsResult.FailureCode);

        var analytics = analyticsResult.Value!;
        var coreCharts = BuildCoreCharts(analytics);
        var coreKpis = BuildCoreKpis(analytics);

        var snapshot = BuildAnalyticsSnapshotJson(analytics);

        var system = AiPromptEngine.AnalyticsSystem + " " + AiPromptEngine.ExecutiveSummarySystem;
        var user = AiPromptEngine.BuildAnalyticsUserPrompt(snapshot);

        var completion = await _openAi.CompleteAsync(
            new List<(string, string)> { ("system", system), ("user", user) },
            jsonObjectFormat: true,
            cancellationToken).ConfigureAwait(false);

        if (!completion.IsSuccess)
            return Result<AiAnalyzeReportsResponseDto>.Fail(completion.Errors, completion.FailureCode);

        try
        {
            var parsed = JsonSerializer.Deserialize<AiAnalyzeEnhancedRawDto>(completion.Value!, AiDeserialize);
            if (parsed == null || string.IsNullOrWhiteSpace(parsed.SummaryAr) || string.IsNullOrWhiteSpace(parsed.SummaryEn))
            {
                return Result<AiAnalyzeReportsResponseDto>.Fail(
                    "AI returned an incomplete analysis.",
                    QuestionnaireErrors.AiProviderError);
            }

            var charts = new List<AiChartSuggestionDto>();
            foreach (var c in parsed.Charts ?? new List<AiChartRawDto>())
            {
                if (c.Labels == null || c.Values == null || c.Labels.Count == 0 || c.Labels.Count != c.Values.Count)
                    continue;
                var kindNorm = (c.Kind ?? "bar").Trim().ToLowerInvariant();
                if (kindNorm != "bar" && kindNorm != "line" && kindNorm != "doughnut")
                    kindNorm = "bar";

                var titleAr = FirstNonEmpty(c.TitleAr, c.Title);
                var titleEn = FirstNonEmpty(c.TitleEn, c.Title);
                if (string.IsNullOrWhiteSpace(titleAr))
                    titleAr = titleEn;
                if (string.IsNullOrWhiteSpace(titleEn))
                    titleEn = titleAr;

                charts.Add(new AiChartSuggestionDto
                {
                    TitleAr = titleAr ?? string.Empty,
                    TitleEn = titleEn ?? string.Empty,
                    Title = titleEn ?? titleAr ?? string.Empty,
                    Kind = kindNorm,
                    Labels = c.Labels,
                    Values = c.Values,
                });

                if (charts.Count >= 4)
                    break;
            }

            var kpis = NormalizeKpis(parsed.Kpis);
            var insightCards = NormalizeInsightCards(parsed.InsightCards);
            var recAr = NormalizeStringList(parsed.RecommendationsAr, 8);
            var recEn = NormalizeStringList(parsed.RecommendationsEn, 8);
            var stepsAr = NormalizeStringList(parsed.ActionPlanStepsAr, 7);
            var stepsEn = NormalizeStringList(parsed.ActionPlanStepsEn, 7);

            // Prefer deterministic charts (always available) then append AI charts (if any).
            var mergedCharts = new List<AiChartSuggestionDto>();
            mergedCharts.AddRange(coreCharts);
            foreach (var c in charts)
            {
                if (mergedCharts.Count >= 6) break;
                mergedCharts.Add(c);
            }

            // Deterministic KPIs first, then AI KPIs.
            var mergedKpis = new List<AiKpiChipDto>();
            mergedKpis.AddRange(coreKpis);
            mergedKpis.AddRange(kpis);

            return Result<AiAnalyzeReportsResponseDto>.Ok(new AiAnalyzeReportsResponseDto
            {
                SummaryAr = parsed.SummaryAr,
                SummaryEn = parsed.SummaryEn,
                Charts = mergedCharts,
                Kpis = mergedKpis,
                InsightCards = insightCards,
                RecommendationsAr = recAr,
                RecommendationsEn = recEn,
                ExecutiveBoxAr = parsed.ExecutiveBoxAr?.Trim() ?? string.Empty,
                ExecutiveBoxEn = parsed.ExecutiveBoxEn?.Trim() ?? string.Empty,
                ActionPlanStepsAr = stepsAr,
                ActionPlanStepsEn = stepsEn,
            });
        }
        catch (Exception ex)
        {
            return Result<AiAnalyzeReportsResponseDto>.Fail(
                ex.Message,
                QuestionnaireErrors.AiProviderError);
        }
    }

    private static List<AiChartSuggestionDto> BuildCoreCharts(CrossSurveyAnalyticsDto a)
    {
        var charts = new List<AiChartSuggestionDto>();

        // 1) Responses over time (line)
        if (a.SubmissionsByDay.Count > 0)
        {
            charts.Add(new AiChartSuggestionDto
            {
                TitleAr = "الردود عبر الوقت",
                TitleEn = "Submissions over time",
                Title = "Submissions over time",
                Kind = "line",
                Labels = a.SubmissionsByDay.Select(p => p.Date).ToList(),
                Values = a.SubmissionsByDay.Select(p => (double)p.Count).ToList(),
            });
        }

        // 2) Response status (doughnut)
        if (a.ResponseStatusDistribution.Count > 0)
        {
            charts.Add(new AiChartSuggestionDto
            {
                TitleAr = "توزيع حالة الردود",
                TitleEn = "Response status distribution",
                Title = "Response status distribution",
                Kind = "doughnut",
                Labels = a.ResponseStatusDistribution.Select(x => x.Key).ToList(),
                Values = a.ResponseStatusDistribution.Select(x => (double)x.Count).ToList(),
            });
        }

        // 3) Ratings distribution (bar)
        if (a.RatingsDistribution.Count > 0)
        {
            charts.Add(new AiChartSuggestionDto
            {
                TitleAr = "توزيع التقييمات",
                TitleEn = "Ratings distribution",
                Title = "Ratings distribution",
                Kind = "bar",
                Labels = a.RatingsDistribution.Select(x => x.Rating.ToString()).ToList(),
                Values = a.RatingsDistribution.Select(x => (double)x.Count).ToList(),
            });

            // 4) Positive vs negative (derived)
            var pos = a.RatingsDistribution.Where(x => x.Rating >= 4).Sum(x => x.Count);
            var neu = a.RatingsDistribution.Where(x => x.Rating == 3).Sum(x => x.Count);
            var neg = a.RatingsDistribution.Where(x => x.Rating <= 2).Sum(x => x.Count);
            var total = pos + neu + neg;
            if (total > 0)
            {
                charts.Add(new AiChartSuggestionDto
                {
                    TitleAr = "إيجابي / محايد / سلبي",
                    TitleEn = "Positive / Neutral / Negative",
                    Title = "Positive / Neutral / Negative",
                    Kind = "doughnut",
                    Labels = new List<string> { "Positive", "Neutral", "Negative" },
                    Values = new List<double> { pos, neu, neg },
                });
            }
        }

        return charts;
    }

    private static List<AiKpiChipDto> BuildCoreKpis(CrossSurveyAnalyticsDto a)
    {
        var kpis = new List<AiKpiChipDto>();

        if (a.RatingsDistribution.Count == 0)
            return kpis;

        var pos = a.RatingsDistribution.Where(x => x.Rating >= 4).Sum(x => x.Count);
        var neu = a.RatingsDistribution.Where(x => x.Rating == 3).Sum(x => x.Count);
        var neg = a.RatingsDistribution.Where(x => x.Rating <= 2).Sum(x => x.Count);
        var total = pos + neu + neg;
        if (total <= 0) return kpis;

        string Pct(int part) => $"{Math.Round(part * 100.0 / total, 1):0.#}%";

        kpis.Add(new AiKpiChipDto
        {
            LabelAr = "إيجابي",
            LabelEn = "Positive",
            ValueText = Pct(pos),
            HintAr = "نسبة التقييمات 4–5",
            HintEn = "Share of ratings 4–5",
        });
        kpis.Add(new AiKpiChipDto
        {
            LabelAr = "سلبي",
            LabelEn = "Negative",
            ValueText = Pct(neg),
            HintAr = "نسبة التقييمات 1–2",
            HintEn = "Share of ratings 1–2",
        });
        kpis.Add(new AiKpiChipDto
        {
            LabelAr = "محايد",
            LabelEn = "Neutral",
            ValueText = Pct(neu),
            HintAr = "نسبة التقييم 3",
            HintEn = "Share of rating 3",
        });

        return kpis;
    }

    private async Task<Result<string>> BuildSuggestContextForAiAsync(
        CrossSurveyAnalyticsFilterRequest filter,
        CancellationToken cancellationToken)
    {
        // AI only needs a small evidence sample (loading full AnswerDetails can be huge/slow).
        filter.IncludeAnswerDetails = false;

        var analyticsResult = await _reports.GetCrossSurveyAnalyticsAsync(filter, cancellationToken).ConfigureAwait(false);
        if (!analyticsResult.IsSuccess)
            return Result<string>.Fail(analyticsResult.Errors, analyticsResult.FailureCode);

        var analytics = analyticsResult.Value!;
        var answerSamples = await LoadAnswerEvidenceSamplesAsync(filter, cancellationToken).ConfigureAwait(false);
        var focusSurveyId = filter.SurveyId;
        var sb = new StringBuilder();
        sb.Append("{\"intent\":\"ai_suggestion\",\"focusSurveyId\":");
        sb.Append(focusSurveyId.HasValue ? $"\"{focusSurveyId}\"" : "null");
        sb.Append(",\"evidence\":\"cross_survey_analytics\"}\n");
        sb.Append(BuildSuggestSnapshotJson(analytics, answerSamples));

        if (focusSurveyId.HasValue)
        {
            sb.Append('\n');
            sb.Append(await BuildFocusedSurveyQuestionsJsonAsync(focusSurveyId.Value, cancellationToken).ConfigureAwait(false));
        }
        else
        {
            sb.Append('\n');
            sb.Append(await BuildRecentSurveysJsonAsync(cancellationToken).ConfigureAwait(false));
        }

        return Result<string>.Ok(sb.ToString());
    }

    /// <summary>
    /// Minimal evidence: applied filter + text answer excerpts only (no dashboard KPIs). Used for sentiment, recommendation, and action-plan drafts.
    /// </summary>
    private async Task<Result<(string Json, int SampleCount)>> BuildAnswerExcerptEvidenceForAiAsync(
        CrossSurveyAnalyticsFilterRequest filter,
        string evidenceIntent,
        CancellationToken cancellationToken)
    {
        filter.IncludeAnswerDetails = false;

        var analyticsResult = await _reports.GetCrossSurveyAnalyticsAsync(filter, cancellationToken).ConfigureAwait(false);
        if (!analyticsResult.IsSuccess)
            return Result<(string Json, int SampleCount)>.Fail(analyticsResult.Errors, analyticsResult.FailureCode);

        var answerSamples = await LoadAnswerEvidenceSamplesAsync(filter, cancellationToken).ConfigureAwait(false);
        var payload = new AnswerExcerptEvidenceDto
        {
            Intent = string.IsNullOrWhiteSpace(evidenceIntent) ? "answer_excerpts_only" : evidenceIntent.Trim(),
            AppliedFilter = analyticsResult.Value!.AppliedFilter,
            AnswerSamples = answerSamples,
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });

        return Result<(string Json, int SampleCount)>.Ok((json, answerSamples.Count));
    }

    private static string NormalizeSentimentSummaryForDisplay(string? s)
    {
        var t = (s ?? string.Empty).Trim();
        if (t.Length == 0)
            return t;
        if (t.Contains('<', StringComparison.Ordinal))
            return t;
        return "<p>" + WebUtility.HtmlEncode(t).Replace("\n", "<br/>", StringComparison.Ordinal) + "</p>";
    }

    private static (int Positive, int Negative, int Neutral) CoerceSentimentCountsToSampleSize(
        int positive,
        int negative,
        int neutral,
        int sampleSize)
    {
        var p = Math.Max(0, positive);
        var n = Math.Max(0, negative);
        var z = Math.Max(0, neutral);
        var sum = p + n + z;
        if (sampleSize <= 0 || sum <= 0)
            return (0, 0, 0);
        if (sum == sampleSize)
            return (p, n, z);

        // Largest remainder method so small buckets keep at least one slot when proportions warrant it.
        var exact = new[]
        {
            (double)p / sum * sampleSize,
            (double)n / sum * sampleSize,
            (double)z / sum * sampleSize,
        };
        var floors = new[] { (int)Math.Floor(exact[0]), (int)Math.Floor(exact[1]), (int)Math.Floor(exact[2]) };
        var rem = sampleSize - floors[0] - floors[1] - floors[2];
        var order = new[] { 0, 1, 2 }.OrderByDescending(i => exact[i] - floors[i]).ToArray();
        for (var i = 0; i < rem; i++)
            floors[order[i]]++;

        return (floors[0], floors[1], floors[2]);
    }

    private static string BuildSuggestSnapshotJson(CrossSurveyAnalyticsDto d, List<AnswerEvidenceRowDto> answerSamples)
    {
        if (answerSamples.Count > MaxSuggestAnswerSampleRows)
            answerSamples = answerSamples.Take(MaxSuggestAnswerSampleRows).ToList();

        var slim = new SuggestAnalyticsSlimDto
        {
            Filter = d.AppliedFilter,
            Overview = d.Overview,
            SurveyStatusDistribution = TakeNamed(d.SurveyStatusDistribution, 20),
            ResponseStatusDistribution = TakeNamed(d.ResponseStatusDistribution, 20),
            AudienceScopeDistribution = TakeNamed(d.AudienceScopeDistribution, 20),
            ParticipantStatusDistribution = TakeNamed(d.ParticipantStatusDistribution, 20),
            SubmissionsByDayOfWeek = TakeNamed(d.SubmissionsByDayOfWeek, 14),
            SubmissionsByDay = TrimTimelineTail(d.SubmissionsByDay, 60),
            TopSurveysBySubmissions = d.TopSurveysBySubmissions.Take(15).ToList(),
            ActionPlanStatusDistribution = TakeNamed(d.ActionPlanStatusDistribution, 20),
            InitiativeStatusDistribution = TakeNamed(d.InitiativeStatusDistribution, 20),
            RatingsDistribution = d.RatingsDistribution.Take(30).ToList(),
            QuestionTypeAnswerTotals = TakeNamed(d.QuestionTypeAnswerTotals, 30),
            TextAnswerKeywords = d.TextAnswerKeywords.Take(40).ToList(),
            AnswerSamples = answerSamples,
        };

        return JsonSerializer.Serialize(slim, new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
    }

    private async Task<List<AnswerEvidenceRowDto>> LoadAnswerEvidenceSamplesAsync(
        CrossSurveyAnalyticsFilterRequest filter,
        CancellationToken cancellationToken)
    {
        const int maxRawRows = 750; // keep fast, enough diversity

        var q = from a in _db.QuestionAnswers.AsNoTracking()
                join r in _db.SurveyResponses.AsNoTracking() on a.ResponseId equals r.Id
                join s in _db.Surveys.AsNoTracking() on r.SurveyId equals s.Id
                join qu in _db.Questions.AsNoTracking() on a.QuestionId equals qu.Id
                where r.Status == ResponseStatus.Submitted
                select new
                {
                    r.SurveyId,
                    r.SubmittedAtUtc,
                    SurveyTitleAr = s.TitleAr,
                    SurveyTitleEn = s.TitleEn,
                    QuestionTitleAr = qu.TitleAr,
                    QuestionTitleEn = qu.TitleEn,
                    QuestionType = qu.Type,
                    qu.OptionsJson,
                    a.ValueJson,
                };

        if (filter.SurveyId.HasValue)
            q = q.Where(x => x.SurveyId == filter.SurveyId.Value);

        if (filter.FromUtc.HasValue)
            q = q.Where(x => x.SubmittedAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            q = q.Where(x => x.SubmittedAtUtc <= filter.ToUtc.Value);

        var raw = await q
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Take(maxRawRows)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var rows = raw.Select(x =>
            {
                var (ar, en) = CrossSurveyAnswerDetailFormatter.FormatAnswer(x.QuestionType, x.OptionsJson, x.ValueJson);
                var qTitle = FirstNonEmpty(x.QuestionTitleEn, x.QuestionTitleAr) ?? string.Empty;
                var excerpt = FirstNonEmpty(en, ar) ?? string.Empty;
                excerpt = TruncateForAiSuggest(excerpt, MaxSuggestAnswerExcerptChars);
                return new AnswerEvidenceRowDto
                {
                    QuestionTitle = qTitle,
                    QuestionType = x.QuestionType.ToString(),
                    AnswerExcerpt = excerpt,
                };
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.AnswerExcerpt))
            .Take(MaxSuggestAnswerSampleRows)
            .ToList();

        return rows;
    }

    private static string TruncateForAiSuggest(string s, int maxChars)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;
        var t = s.Trim();
        return t.Length <= maxChars ? t : t[..maxChars] + "…";
    }

    private async Task<string> BuildRecentSurveysJsonAsync(CancellationToken cancellationToken)
    {
        var surveyRows = await _db.Surveys.AsNoTracking()
            .Where(s => s.RecordStatus == RecordStatus.Active)
            .OrderByDescending(s => s.PublishedAtUtc ?? s.CreatedOnUtc)
            .Take(25)
            .Select(s => new
            {
                s.Id,
                s.TitleAr,
                s.TitleEn,
                s.Status,
                Submitted = s.Responses.Count(r => r.Status == ResponseStatus.Submitted),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.Append("{\"recentSurveys\":[");
        for (var i = 0; i < surveyRows.Count; i++)
        {
            var r = surveyRows[i];
            if (i > 0) sb.Append(',');
            sb.Append(
                $"{{\"id\":\"{r.Id}\",\"titleAr\":{JsonEncoded(r.TitleAr)},\"titleEn\":{JsonEncoded(r.TitleEn)},\"status\":\"{r.Status}\",\"submittedResponses\":{r.Submitted}}}");
        }

        sb.Append("]}");
        return sb.ToString();
    }

    private async Task<string> BuildFocusedSurveyQuestionsJsonAsync(Guid surveyId, CancellationToken cancellationToken)
    {
        var qTitles = await _db.Questions.AsNoTracking()
            .Where(q => q.SurveyId == surveyId)
            .OrderBy(q => q.DisplayOrder)
            .Take(40)
            .Select(q => new { q.TitleAr, q.TitleEn, q.Type })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.Append("{\"focusedSurveyQuestions\":[");
        for (var i = 0; i < qTitles.Count; i++)
        {
            var q = qTitles[i];
            if (i > 0) sb.Append(',');
            sb.Append(
                $"{{\"titleAr\":{JsonEncoded(q.TitleAr)},\"titleEn\":{JsonEncoded(q.TitleEn)},\"type\":\"{q.Type}\"}}");
        }

        sb.Append("]}");
        return sb.ToString();
    }

    private static string JsonEncoded(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return "\"\"";
        return JsonSerializer.Serialize(s);
    }

    private static string BuildAnalyticsSnapshotJson(CrossSurveyAnalyticsDto d)
    {
        var slim = new AnalyticsSlimDto
        {
            Filter = d.AppliedFilter,
            Overview = d.Overview,
            SurveyStatusDistribution = TakeNamed(d.SurveyStatusDistribution, 20),
            ResponseStatusDistribution = TakeNamed(d.ResponseStatusDistribution, 20),
            AudienceScopeDistribution = TakeNamed(d.AudienceScopeDistribution, 20),
            ParticipantStatusDistribution = TakeNamed(d.ParticipantStatusDistribution, 20),
            SubmissionsByDayOfWeek = TakeNamed(d.SubmissionsByDayOfWeek, 14),
            SubmissionsByDay = TrimTimelineTail(d.SubmissionsByDay, 60),
            TopSurveysBySubmissions = d.TopSurveysBySubmissions.Take(15).ToList(),
            ActionPlanStatusDistribution = TakeNamed(d.ActionPlanStatusDistribution, 20),
            InitiativeStatusDistribution = TakeNamed(d.InitiativeStatusDistribution, 20),
            RatingsDistribution = d.RatingsDistribution.Take(30).ToList(),
            QuestionTypeAnswerTotals = TakeNamed(d.QuestionTypeAnswerTotals, 30),
            TextAnswerKeywords = d.TextAnswerKeywords.Take(40).ToList(),
        };

        return JsonSerializer.Serialize(slim, new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
    }

    private static IReadOnlyList<NamedCountDto> TakeNamed(IReadOnlyList<NamedCountDto> src, int max) =>
        src.Count <= max ? src : src.Take(max).ToList();

    private static IReadOnlyList<TimelinePointDto> TrimTimelineTail(IReadOnlyList<TimelinePointDto> src, int max)
    {
        if (src.Count <= max)
            return src;
        return src.Skip(src.Count - max).ToList();
    }

    private sealed class AnalyticsSlimDto
    {
        public CrossSurveyAnalyticsFilterSnapshotDto Filter { get; set; } = new();
        public CrossSurveyOverviewDto Overview { get; set; } = new();
        public IReadOnlyList<NamedCountDto> SurveyStatusDistribution { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<NamedCountDto> ResponseStatusDistribution { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<NamedCountDto> AudienceScopeDistribution { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<NamedCountDto> ParticipantStatusDistribution { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<NamedCountDto> SubmissionsByDayOfWeek { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<TimelinePointDto> SubmissionsByDay { get; set; } = Array.Empty<TimelinePointDto>();
        public IReadOnlyList<TopSurveyRowDto> TopSurveysBySubmissions { get; set; } = Array.Empty<TopSurveyRowDto>();
        public IReadOnlyList<NamedCountDto> ActionPlanStatusDistribution { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<NamedCountDto> InitiativeStatusDistribution { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<RatingAnalyticsDto> RatingsDistribution { get; set; } = Array.Empty<RatingAnalyticsDto>();
        public IReadOnlyList<NamedCountDto> QuestionTypeAnswerTotals { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<KeywordCountDto> TextAnswerKeywords { get; set; } = Array.Empty<KeywordCountDto>();
    }

    /// <summary>Analytics snapshot plus capped answer excerpts for AI recommendation / action-plan prompts.</summary>
    private sealed class SuggestAnalyticsSlimDto
    {
        public CrossSurveyAnalyticsFilterSnapshotDto Filter { get; set; } = new();
        public CrossSurveyOverviewDto Overview { get; set; } = new();
        public IReadOnlyList<NamedCountDto> SurveyStatusDistribution { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<NamedCountDto> ResponseStatusDistribution { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<NamedCountDto> AudienceScopeDistribution { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<NamedCountDto> ParticipantStatusDistribution { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<NamedCountDto> SubmissionsByDayOfWeek { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<TimelinePointDto> SubmissionsByDay { get; set; } = Array.Empty<TimelinePointDto>();
        public IReadOnlyList<TopSurveyRowDto> TopSurveysBySubmissions { get; set; } = Array.Empty<TopSurveyRowDto>();
        public IReadOnlyList<NamedCountDto> ActionPlanStatusDistribution { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<NamedCountDto> InitiativeStatusDistribution { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<RatingAnalyticsDto> RatingsDistribution { get; set; } = Array.Empty<RatingAnalyticsDto>();
        public IReadOnlyList<NamedCountDto> QuestionTypeAnswerTotals { get; set; } = Array.Empty<NamedCountDto>();
        public IReadOnlyList<KeywordCountDto> TextAnswerKeywords { get; set; } = Array.Empty<KeywordCountDto>();
        public List<AnswerEvidenceRowDto> AnswerSamples { get; set; } = new();
    }

    private sealed class AnswerExcerptEvidenceDto
    {
        public string Intent { get; init; } = "answer_excerpts_only";

        public CrossSurveyAnalyticsFilterSnapshotDto AppliedFilter { get; init; } = new();

        public List<AnswerEvidenceRowDto> AnswerSamples { get; init; } = new();
    }

    private sealed class AnswerEvidenceRowDto
    {
        public string QuestionTitle { get; set; } = "";
        public string QuestionType { get; set; } = "";
        public string AnswerExcerpt { get; set; } = "";
    }

    private sealed class AiAnalyzeEnhancedRawDto
    {
        public string SummaryAr { get; set; } = "";
        public string SummaryEn { get; set; } = "";
        public List<AiChartRawDto>? Charts { get; set; } = new();
        public List<AiKpiRawDto>? Kpis { get; set; }
        public List<AiInsightCardRawDto>? InsightCards { get; set; }
        public List<string>? RecommendationsAr { get; set; }
        public List<string>? RecommendationsEn { get; set; }
        public string? ExecutiveBoxAr { get; set; }
        public string? ExecutiveBoxEn { get; set; }
        public List<string>? ActionPlanStepsAr { get; set; }
        public List<string>? ActionPlanStepsEn { get; set; }
    }

    private sealed class AiKpiRawDto
    {
        public string? LabelAr { get; set; }
        public string? LabelEn { get; set; }
        public string? ValueText { get; set; }
        public string? HintAr { get; set; }
        public string? HintEn { get; set; }
    }

    private sealed class AiInsightCardRawDto
    {
        public string? Kind { get; set; }
        public string? TitleAr { get; set; }
        public string? TitleEn { get; set; }
        public string? BodyAr { get; set; }
        public string? BodyEn { get; set; }
        public string? Severity { get; set; }
    }

    private sealed class AiChartRawDto
    {
        public string? Title { get; set; }
        public string? TitleAr { get; set; }
        public string? TitleEn { get; set; }
        public string? Kind { get; set; }
        public List<string>? Labels { get; set; }
        public List<double>? Values { get; set; }
    }

    /// <summary>Validates AI secondary sentiment chart (bar/line only).</summary>
    private static AiChartSuggestionDto? NormalizeSentimentInsightChart(AiChartRawDto? c)
    {
        if (c?.Labels == null || c.Values == null || c.Labels.Count == 0 || c.Labels.Count != c.Values.Count)
            return null;

        const int maxPoints = 12;
        var pairs = new List<(string Label, double Value)>();
        for (var i = 0; i < Math.Min(c.Labels.Count, c.Values.Count) && pairs.Count < maxPoints; i++)
        {
            var label = (c.Labels[i] ?? string.Empty).Trim();
            if (label.Length == 0)
                continue;
            pairs.Add((label, c.Values[i]));
        }

        if (pairs.Count == 0)
            return null;

        var kindNorm = (c.Kind ?? "bar").Trim().ToLowerInvariant();
        if (kindNorm == "doughnut")
            kindNorm = "bar";
        if (kindNorm != "bar" && kindNorm != "line")
            kindNorm = "bar";

        var titleAr = FirstNonEmpty(c.TitleAr, c.Title);
        var titleEn = FirstNonEmpty(c.TitleEn, c.Title);
        if (string.IsNullOrWhiteSpace(titleAr))
            titleAr = titleEn;
        if (string.IsNullOrWhiteSpace(titleEn))
            titleEn = titleAr;
        if (string.IsNullOrWhiteSpace(titleAr))
            return null;

        return new AiChartSuggestionDto
        {
            TitleAr = titleAr!,
            TitleEn = titleEn!,
            Title = titleEn ?? titleAr ?? string.Empty,
            Kind = kindNorm,
            Labels = pairs.Select(p => p.Label).ToList(),
            Values = pairs.Select(p => p.Value).ToList(),
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return null;
    }

    private static IReadOnlyList<string> NormalizeStringList(IReadOnlyList<string>? src, int max)
    {
        if (src == null || src.Count == 0)
            return Array.Empty<string>();
        var list = new List<string>();
        foreach (var s in src)
        {
            var t = (s ?? string.Empty).Trim();
            if (t.Length == 0)
                continue;
            list.Add(t);
            if (list.Count >= max)
                break;
        }

        return list;
    }

    private static IReadOnlyList<AiKpiChipDto> NormalizeKpis(IReadOnlyList<AiKpiRawDto>? src)
    {
        if (src == null || src.Count == 0)
            return Array.Empty<AiKpiChipDto>();
        var list = new List<AiKpiChipDto>();
        foreach (var k in src)
        {
            var la = (k.LabelAr ?? string.Empty).Trim();
            var le = (k.LabelEn ?? string.Empty).Trim();
            var vt = (k.ValueText ?? string.Empty).Trim();
            if (la.Length == 0 && le.Length == 0)
                continue;
            if (string.IsNullOrEmpty(la))
                la = le;
            if (string.IsNullOrEmpty(le))
                le = la;
            list.Add(new AiKpiChipDto
            {
                LabelAr = la,
                LabelEn = le,
                ValueText = vt.Length > 0 ? vt : "—",
                HintAr = string.IsNullOrWhiteSpace(k.HintAr) ? null : k.HintAr.Trim(),
                HintEn = string.IsNullOrWhiteSpace(k.HintEn) ? null : k.HintEn.Trim(),
            });
            if (list.Count >= 6)
                break;
        }

        return list;
    }

    private static IReadOnlyList<AiInsightCardDto> NormalizeInsightCards(IReadOnlyList<AiInsightCardRawDto>? src)
    {
        if (src == null || src.Count == 0)
            return Array.Empty<AiInsightCardDto>();
        var list = new List<AiInsightCardDto>();
        foreach (var c in src)
        {
            var titleAr = (c.TitleAr ?? string.Empty).Trim();
            var titleEn = (c.TitleEn ?? string.Empty).Trim();
            var bodyAr = (c.BodyAr ?? string.Empty).Trim();
            var bodyEn = (c.BodyEn ?? string.Empty).Trim();
            if (titleAr.Length == 0 && titleEn.Length == 0 && bodyAr.Length == 0 && bodyEn.Length == 0)
                continue;
            if (string.IsNullOrEmpty(titleAr))
                titleAr = titleEn;
            if (string.IsNullOrEmpty(titleEn))
                titleEn = titleAr;
            if (string.IsNullOrEmpty(bodyAr))
                bodyAr = bodyEn;
            if (string.IsNullOrEmpty(bodyEn))
                bodyEn = bodyAr;
            list.Add(new AiInsightCardDto
            {
                Kind = NormalizeInsightKind(c.Kind),
                TitleAr = titleAr,
                TitleEn = titleEn,
                BodyAr = bodyAr,
                BodyEn = bodyEn,
                Severity = NormalizeSeverity(c.Severity),
            });
            if (list.Count >= 8)
                break;
        }

        return list;
    }

    private static string NormalizeInsightKind(string? raw)
    {
        var k = (raw ?? string.Empty).Trim().ToLowerInvariant().Replace(' ', '_');
        return k switch
        {
            "risk_detected" or "risk" => "risk_detected",
            "low_satisfaction" => "low_satisfaction",
            "improvement_opportunity" or "opportunity" => "improvement_opportunity",
            "executive_insight" or "insight" => "executive_insight",
            "recommended_action" or "action" => "recommended_action",
            "sentiment_summary" or "sentiment" => "sentiment_summary",
            _ => "executive_insight",
        };
    }

    private static string? NormalizeSeverity(string? raw)
    {
        var s = (raw ?? string.Empty).Trim().ToLowerInvariant();
        return s switch
        {
            "low" or "medium" or "high" => s,
            _ => null,
        };
    }

    /// <summary>
    /// Models sometimes wrap HTML in markdown code fences despite instructions.
    /// </summary>
    private static string StripMarkdownCodeFences(string text)
    {
        var t = text.Trim();
        if (!t.StartsWith("```", StringComparison.Ordinal))
            return text.Trim();

        var i = 3;
        while (i < t.Length && t[i] is not ('\n' or '\r'))
            i++;
        while (i < t.Length && t[i] is '\n' or '\r')
            i++;

        var body = t[i..];
        var close = body.LastIndexOf("```", StringComparison.Ordinal);
        if (close >= 0)
            body = body[..close];

        return body.Trim();
    }
}
