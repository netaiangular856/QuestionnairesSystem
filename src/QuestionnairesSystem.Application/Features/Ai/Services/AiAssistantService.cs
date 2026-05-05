using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Ai.DTOs;
using QuestionnairesSystem.Application.Features.Ai.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Ai.Services;

public sealed class AiAssistantService : IAiAssistantService
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

    public AiAssistantService(
        OpenAiChatClient openAi,
        IQuestionnaireReportService reports,
        QuestionnairesDbContext db)
    {
        _openAi = openAi;
        _reports = reports;
        _db = db;
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
        var contextResult = await BuildSuggestContextForAiAsync(request.SurveyId, cancellationToken).ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<AiSuggestRecommendationDraftDto>.Fail(contextResult.Errors, contextResult.FailureCode);

        var context = contextResult.Value!;

        var system =
            "You are an enterprise improvement advisor. You MUST ground the recommendation ONLY in the evidence JSON blocks below " +
            "(submitted-response analytics: overview counts, distributions, rating buckets, text keywords, question types, and answer excerpts). " +
            "Reference concrete patterns visible in the data (e.g. dominant ratings, recurring short-text themes, low submission volume). " +
            "Do not invent submission counts or metrics not present. If the evidence is thin, say so and recommend measurement or follow-up instead of fabricating facts. " +
            "Respond with JSON only: {\"titleAr\",\"titleEn\",\"descriptionAr\",\"descriptionEn\",\"priority\"}. " +
            "priority must be 3 (low), 5 (medium), or 8 (high). Titles max 500 chars; descriptions max 4000 chars.";
        var user = "Evidence for recommendation:\n" + context;

        var completion = await _openAi.CompleteAsync(
            new List<(string, string)> { ("system", system), ("user", user) },
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
        var contextResult = await BuildSuggestContextForAiAsync(request.SurveyId, cancellationToken).ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<AiSuggestActionPlanDraftDto>.Fail(contextResult.Errors, contextResult.FailureCode);

        var context = contextResult.Value!;

        var system =
            "You are a strategic planning assistant. You MUST base the plan ONLY on the evidence JSON below " +
            "(same analytics as the reports dashboard for the chosen scope: overview, distributions, keywords, ratings, answer excerpts). " +
            "Tie initiatives to observable gaps or strengths in the data. Do not invent KPIs. If data is insufficient, state what to measure next. " +
            "Respond with JSON only: {\"titleAr\",\"titleEn\",\"descriptionAr\",\"descriptionEn\"}. " +
            "Titles max 500 chars; descriptions max 4000 chars.";
        var user = "Evidence for action plan:\n" + context;

        var completion = await _openAi.CompleteAsync(
            new List<(string, string)> { ("system", system), ("user", user) },
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

        var snapshot = BuildAnalyticsSnapshotJson(analyticsResult.Value!);

        var system =
            "You are a data analyst for survey and execution KPIs. Given ONLY the JSON snapshot, write bilingual summaries " +
            "and suggest up to 4 charts that reflect REAL numbers from the snapshot (do not invent metrics). " +
            "Respond with JSON only: {\"summaryAr\",\"summaryEn\",\"charts\":[{\"titleAr\",\"titleEn\",\"kind\",\"labels\",\"values\"}]} " +
            "where each chart MUST include both titleAr (Arabic) and titleEn (English) short titles, kind is bar, line, or doughnut; " +
            "labels and values same length; values numeric.";
        var user = "Analytics snapshot:\n" + snapshot;

        var completion = await _openAi.CompleteAsync(
            new List<(string, string)> { ("system", system), ("user", user) },
            jsonObjectFormat: true,
            cancellationToken).ConfigureAwait(false);

        if (!completion.IsSuccess)
            return Result<AiAnalyzeReportsResponseDto>.Fail(completion.Errors, completion.FailureCode);

        try
        {
            var parsed = JsonSerializer.Deserialize<AiAnalyzeRawDto>(completion.Value!, AiDeserialize);
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

            return Result<AiAnalyzeReportsResponseDto>.Ok(new AiAnalyzeReportsResponseDto
            {
                SummaryAr = parsed.SummaryAr,
                SummaryEn = parsed.SummaryEn,
                Charts = charts,
            });
        }
        catch (Exception ex)
        {
            return Result<AiAnalyzeReportsResponseDto>.Fail(
                ex.Message,
                QuestionnaireErrors.AiProviderError);
        }
    }

    private async Task<Result<string>> BuildSuggestContextForAiAsync(Guid? surveyId, CancellationToken cancellationToken)
    {
        var filter = new CrossSurveyAnalyticsFilterRequest
        {
            SurveyId = surveyId,
            IncludeAnswerDetails = surveyId.HasValue,
        };

        var analyticsResult = await _reports.GetCrossSurveyAnalyticsAsync(filter, cancellationToken).ConfigureAwait(false);
        if (!analyticsResult.IsSuccess)
            return Result<string>.Fail(analyticsResult.Errors, analyticsResult.FailureCode);

        var analytics = analyticsResult.Value!;
        var sb = new StringBuilder();
        sb.Append("{\"intent\":\"ai_suggestion\",\"focusSurveyId\":");
        sb.Append(surveyId.HasValue ? $"\"{surveyId}\"" : "null");
        sb.Append(",\"evidence\":\"cross_survey_analytics\"}\n");
        sb.Append(BuildSuggestSnapshotJson(analytics));

        if (surveyId.HasValue)
        {
            sb.Append('\n');
            sb.Append(await BuildFocusedSurveyQuestionsJsonAsync(surveyId.Value, cancellationToken).ConfigureAwait(false));
        }
        else
        {
            sb.Append('\n');
            sb.Append(await BuildRecentSurveysJsonAsync(cancellationToken).ConfigureAwait(false));
        }

        return Result<string>.Ok(sb.ToString());
    }

    private static string BuildSuggestSnapshotJson(CrossSurveyAnalyticsDto d)
    {
        var answerSamples = d.AnswerDetails
            .Take(MaxSuggestAnswerSampleRows)
            .Select(row =>
            {
                var qTitle = FirstNonEmpty(row.QuestionTitleEn, row.QuestionTitleAr) ?? string.Empty;
                var excerpt = FirstNonEmpty(row.AnswerTextEn, row.AnswerTextAr) ?? string.Empty;
                excerpt = TruncateForAiSuggest(excerpt, MaxSuggestAnswerExcerptChars);
                return new AnswerEvidenceRowDto
                {
                    QuestionTitle = qTitle,
                    QuestionType = row.QuestionTypeKey,
                    AnswerExcerpt = excerpt,
                };
            })
            .ToList();

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

    private sealed class AnswerEvidenceRowDto
    {
        public string QuestionTitle { get; set; } = "";
        public string QuestionType { get; set; } = "";
        public string AnswerExcerpt { get; set; } = "";
    }

    private sealed class AiAnalyzeRawDto
    {
        public string SummaryAr { get; set; } = "";
        public string SummaryEn { get; set; } = "";
        public List<AiChartRawDto>? Charts { get; set; } = new();
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

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return null;
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
