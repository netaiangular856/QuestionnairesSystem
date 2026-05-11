using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Ai.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Ai.Services;

public sealed partial class AiAssistantService
{
    private const int MaxCopilotHistory = 10;
    private const int MaxUserMessageChars = 4000;
    private const int MaxSurveyBriefChars = 8000;

    private static readonly JsonSerializerOptions CopilotDeserialize = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public async Task<Result<AiGeneratedSurveyDraftDto>> GenerateSurveyDraftAsync(
        AiGenerateSurveyRequest request,
        CancellationToken cancellationToken = default)
    {
        var ar = (request.BriefAr ?? string.Empty).Trim();
        var en = (request.BriefEn ?? string.Empty).Trim();
        if (ar.Length == 0 && en.Length == 0)
        {
            return Result<AiGeneratedSurveyDraftDto>.Fail(
                "Provide a survey goal in Arabic and/or English.",
                QuestionnaireErrors.AiInsufficientData);
        }

        if (ar.Length > MaxSurveyBriefChars || en.Length > MaxSurveyBriefChars)
        {
            return Result<AiGeneratedSurveyDraftDto>.Fail(
                $"Brief exceeds maximum length ({MaxSurveyBriefChars}).",
                QuestionnaireErrors.InvalidOperation);
        }

        var maxQ = request.MaxQuestions is >= 3 and <= 20 ? request.MaxQuestions.Value : 12;
        var brief = new StringBuilder();
        if (ar.Length > 0)
            brief.Append("Goal (Arabic):\n").Append(ar).Append('\n');
        if (en.Length > 0)
            brief.Append("Goal (English):\n").Append(en).Append('\n');

        var user = AiPromptEngine.BuildSurveyGenerationUserPrompt(brief.ToString(), maxQ);
        var completion = await _openAi.CompleteAsync(
            new List<(string, string)> { ("system", AiPromptEngine.SurveyBuilderSystem), ("user", user) },
            jsonObjectFormat: true,
            cancellationToken).ConfigureAwait(false);

        if (!completion.IsSuccess)
            return Result<AiGeneratedSurveyDraftDto>.Fail(completion.Errors, completion.FailureCode);

        return DeserializeGeneratedSurveyDraft(completion.Value!, maxQ);
    }

    public async Task<Result<AiAutoCreateSurveyResponseDto>> GenerateAndCreateSurveyFromRecommendationsAsync(
        AiAutoSurveyFromRecommendationsRequest? request,
        CancellationToken cancellationToken = default)
    {
        var maxQ = request?.MaxQuestions is >= 3 and <= 20 ? request.MaxQuestions.Value : 12;
        var maxRecs = Math.Clamp(request?.MaxRecommendations ?? 40, 5, 80);
        var maxSurveys = Math.Clamp(request?.RecentSurveyCount ?? 25, 5, 60);

        var evidenceJson = await BuildRecommendationsAndSurveysEvidenceJsonAsync(maxRecs, maxSurveys, cancellationToken)
            .ConfigureAwait(false);
        if (evidenceJson is null)
        {
            return Result<AiAutoCreateSurveyResponseDto>.Fail(
                "No recommendations or surveys were found to analyze. Add recommendations or surveys first.",
                QuestionnaireErrors.AiInsufficientData);
        }

        var user = AiPromptEngine.BuildAutoSurveyFromRecommendationsUserPrompt(evidenceJson, maxQ);
        var completion = await _openAi.CompleteAsync(
            new List<(string, string)>
            {
                ("system", AiPromptEngine.AutoSurveyFromRecommendationsSystem),
                ("user", user),
            },
            jsonObjectFormat: true,
            cancellationToken).ConfigureAwait(false);

        if (!completion.IsSuccess)
            return Result<AiAutoCreateSurveyResponseDto>.Fail(completion.Errors, completion.FailureCode);

        var draftResult = DeserializeGeneratedSurveyDraft(completion.Value!, maxQ);
        if (!draftResult.IsSuccess)
            return Result<AiAutoCreateSurveyResponseDto>.Fail(draftResult.Errors, draftResult.FailureCode);

        var createReq = MapAiSurveyDraftToCreateRequest(draftResult.Value!);
        var created = await _surveys.CreateAsync(createReq, cancellationToken).ConfigureAwait(false);
        if (!created.IsSuccess)
            return Result<AiAutoCreateSurveyResponseDto>.Fail(created.Errors, created.FailureCode);

        var d = created.Value!;
        return Result<AiAutoCreateSurveyResponseDto>.Ok(new AiAutoCreateSurveyResponseDto
        {
            SurveyId = d.Id,
            TitleAr = d.TitleAr,
            TitleEn = d.TitleEn,
        });
    }

    private async Task<string?> BuildRecommendationsAndSurveysEvidenceJsonAsync(
        int maxRecommendations,
        int recentSurveyCount,
        CancellationToken cancellationToken)
    {
        var recs = await _db.Recommendations.AsNoTracking()
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedOnUtc)
            .Take(maxRecommendations)
            .Select(r => new
            {
                r.TitleAr,
                r.TitleEn,
                r.DescriptionAr,
                r.DescriptionEn,
                r.Priority,
                Status = r.Status.ToString(),
                LinkedSurveyTitleAr = r.Survey != null ? r.Survey.TitleAr : null,
                LinkedSurveyTitleEn = r.Survey != null ? r.Survey.TitleEn : null,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var surveys = await _db.Surveys.AsNoTracking()
            .OrderByDescending(s => s.CreatedOnUtc)
            .Take(recentSurveyCount)
            .Select(s => new { s.TitleAr, s.TitleEn, Status = s.Status.ToString() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (recs.Count == 0 && surveys.Count == 0)
            return null;

        var payload = new { recommendations = recs, recentSurveys = surveys };
        var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return JsonSerializer.Serialize(payload, jsonOpts);
    }

    private static CreateSurveyRequest MapAiSurveyDraftToCreateRequest(AiGeneratedSurveyDraftDto draft)
    {
        var questions = new List<CreateSurveyQuestionItem>();
        var order = 0;
        foreach (var q in draft.Questions)
        {
            if (!Enum.TryParse<QuestionType>(q.Type, ignoreCase: true, out var qt))
                qt = QuestionType.ShortText;

            string? optionsJson = null;
            if (qt is QuestionType.SingleChoice or QuestionType.MultipleChoice)
            {
                var list = new List<Dictionary<string, string>>();
                var i = 0;
                foreach (var o in q.Options ?? Array.Empty<AiGeneratedOptionDraftDto>())
                {
                    var oa = (o.TextAr ?? string.Empty).Trim();
                    var oe = (o.TextEn ?? string.Empty).Trim();
                    if (oa.Length == 0 && oe.Length == 0)
                        continue;
                    if (string.IsNullOrEmpty(oa))
                        oa = oe;
                    if (string.IsNullOrEmpty(oe))
                        oe = oa;
                    i++;
                    list.Add(new Dictionary<string, string>
                    {
                        ["value"] = $"opt_{i}",
                        ["labelAr"] = oa,
                        ["labelEn"] = oe,
                    });
                    if (list.Count >= 7)
                        break;
                }

                if (list.Count >= 2)
                    optionsJson = JsonSerializer.Serialize(list);
                else
                {
                    qt = QuestionType.ShortText;
                    optionsJson = null;
                }
            }

            questions.Add(new CreateSurveyQuestionItem
            {
                Type = qt,
                TitleAr = q.TitleAr.Trim(),
                TitleEn = q.TitleEn.Trim(),
                IsRequired = q.Required,
                OptionsJson = optionsJson,
                DisplayOrder = ++order,
            });
        }

        return new CreateSurveyRequest
        {
            TitleAr = draft.TitleAr.Trim(),
            TitleEn = draft.TitleEn.Trim(),
            DescriptionAr = string.IsNullOrWhiteSpace(draft.DescriptionAr) ? null : draft.DescriptionAr.Trim(),
            DescriptionEn = string.IsNullOrWhiteSpace(draft.DescriptionEn) ? null : draft.DescriptionEn.Trim(),
            Questions = questions,
            AudienceScope = SurveyAudienceScope.AllOrganizationMembers,
        };
    }

    private Result<AiGeneratedSurveyDraftDto> DeserializeGeneratedSurveyDraft(string completionJson, int maxQ)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<AiSurveyGenRawDto>(completionJson, CopilotDeserialize);
            if (raw == null || string.IsNullOrWhiteSpace(raw.TitleAr) || string.IsNullOrWhiteSpace(raw.TitleEn))
            {
                return Result<AiGeneratedSurveyDraftDto>.Fail(
                    "AI returned an incomplete survey draft.",
                    QuestionnaireErrors.AiProviderError);
            }

            var questions = new List<AiGeneratedQuestionDraftDto>();
            foreach (var q in raw.Questions ?? new List<AiSurveyQuestionRawDto>())
            {
                var type = NormalizeQuestionTypeString(q.Type);
                var qAr = (q.TitleAr ?? string.Empty).Trim();
                var qEn = (q.TitleEn ?? string.Empty).Trim();
                if (qAr.Length == 0 && qEn.Length == 0)
                    continue;
                if (string.IsNullOrEmpty(qAr))
                    qAr = qEn;
                if (string.IsNullOrEmpty(qEn))
                    qEn = qAr;

                IReadOnlyList<AiGeneratedOptionDraftDto>? opts = null;
                if (type is "SingleChoice" or "MultipleChoice")
                {
                    var olist = new List<AiGeneratedOptionDraftDto>();
                    foreach (var o in q.Options ?? new List<AiSurveyOptionRawDto>())
                    {
                        var oa = (o.TextAr ?? string.Empty).Trim();
                        var oe = (o.TextEn ?? string.Empty).Trim();
                        if (oa.Length == 0 && oe.Length == 0)
                            continue;
                        if (string.IsNullOrEmpty(oa))
                            oa = oe;
                        if (string.IsNullOrEmpty(oe))
                            oe = oa;
                        olist.Add(new AiGeneratedOptionDraftDto { TextAr = oa, TextEn = oe });
                        if (olist.Count >= 7)
                            break;
                    }

                    if (olist.Count > 0)
                        opts = olist;
                }

                questions.Add(new AiGeneratedQuestionDraftDto
                {
                    Type = type,
                    TitleAr = qAr,
                    TitleEn = qEn,
                    Required = q.Required,
                    Options = opts,
                });
                if (questions.Count >= maxQ)
                    break;
            }

            if (questions.Count == 0)
            {
                return Result<AiGeneratedSurveyDraftDto>.Fail(
                    "AI did not return any usable questions.",
                    QuestionnaireErrors.AiProviderError);
            }

            return Result<AiGeneratedSurveyDraftDto>.Ok(new AiGeneratedSurveyDraftDto
            {
                TitleAr = raw.TitleAr.Trim(),
                TitleEn = raw.TitleEn.Trim(),
                DescriptionAr = string.IsNullOrWhiteSpace(raw.DescriptionAr) ? null : raw.DescriptionAr.Trim(),
                DescriptionEn = string.IsNullOrWhiteSpace(raw.DescriptionEn) ? null : raw.DescriptionEn.Trim(),
                Questions = questions,
            });
        }
        catch (Exception ex)
        {
            return Result<AiGeneratedSurveyDraftDto>.Fail(ex.Message, QuestionnaireErrors.AiProviderError);
        }
    }

    public async Task<Result<AiSentimentAnalysisResponseDto>> AnalyzeSentimentAsync(
        AiSentimentAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        var filter = request.AnalyticsFilter ?? new CrossSurveyAnalyticsFilterRequest();
        if (request.SurveyId.HasValue)
            filter.SurveyId = request.SurveyId;

        // Default window: last 30 days (cross-survey). For single survey, allow the user to omit dates.
        if (!filter.FromUtc.HasValue && !filter.ToUtc.HasValue && !filter.SurveyId.HasValue)
        {
            var days = request.DefaultWindowDays.GetValueOrDefault(30);
            if (days < 1) days = 1;
            if (days > 365) days = 365;
            var now = DateTime.UtcNow;
            filter.ToUtc = now;
            filter.FromUtc = now.AddDays(-days);
        }

        // AI only needs samples, not full export rows (huge). We'll inject our own samples in the context builder.
        filter.IncludeAnswerDetails = false;

        var contextResult = await BuildAnswerExcerptEvidenceForAiAsync(
                filter,
                "sentiment_text_excerpts_only",
                cancellationToken)
            .ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<AiSentimentAnalysisResponseDto>.Fail(contextResult.Errors, contextResult.FailureCode);

        var (evidenceJson, sampleCount) = contextResult.Value!;
        var user = AiPromptEngine.BuildSentimentUserPrompt(evidenceJson);
        var completion = await _openAi.CompleteAsync(
            new List<(string, string)> { ("system", AiPromptEngine.SentimentSystem), ("user", user) },
            jsonObjectFormat: true,
            cancellationToken).ConfigureAwait(false);

        if (!completion.IsSuccess)
            return Result<AiSentimentAnalysisResponseDto>.Fail(completion.Errors, completion.FailureCode);

        try
        {
            var raw = JsonSerializer.Deserialize<AiSentimentRawDto>(completion.Value!, CopilotDeserialize);
            if (raw == null || string.IsNullOrWhiteSpace(raw.SummaryAr) || string.IsNullOrWhiteSpace(raw.SummaryEn))
            {
                return Result<AiSentimentAnalysisResponseDto>.Fail(
                    "AI returned an incomplete sentiment result.",
                    QuestionnaireErrors.AiProviderError);
            }

            var cards = NormalizeInsightCards(raw.Cards);
            var insightChart = NormalizeSentimentInsightChart(raw.InsightChart);
            var pos = Math.Max(0, raw.PositiveCount ?? 0);
            var neg = Math.Max(0, raw.NegativeCount ?? 0);
            var neu = Math.Max(0, raw.NeutralCount ?? 0);
            var (p2, n2, z2) = CoerceSentimentCountsToSampleSize(pos, neg, neu, sampleCount);
            AiSentimentMixDto? mix = null;
            if (sampleCount > 0 && p2 + n2 + z2 == sampleCount && p2 + n2 + z2 > 0)
                mix = new AiSentimentMixDto { Positive = p2, Negative = n2, Neutral = z2 };

            return Result<AiSentimentAnalysisResponseDto>.Ok(new AiSentimentAnalysisResponseDto
            {
                SummaryAr = NormalizeSentimentSummaryForDisplay(raw.SummaryAr.Trim()),
                SummaryEn = NormalizeSentimentSummaryForDisplay(raw.SummaryEn.Trim()),
                OverallToneAr = string.IsNullOrWhiteSpace(raw.OverallToneAr) ? null : raw.OverallToneAr.Trim(),
                OverallToneEn = string.IsNullOrWhiteSpace(raw.OverallToneEn) ? null : raw.OverallToneEn.Trim(),
                SentimentMix = mix,
                InsightChart = insightChart,
                Cards = cards,
            });
        }
        catch (Exception ex)
        {
            return Result<AiSentimentAnalysisResponseDto>.Fail(ex.Message, QuestionnaireErrors.AiProviderError);
        }
    }

    public async Task<Result<AiCopilotChatResponseDto>> CopilotChatAsync(
        AiCopilotChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var msg = (request.UserMessage ?? string.Empty).Trim();
        if (msg.Length == 0)
        {
            return Result<AiCopilotChatResponseDto>.Fail(
                "User message is required.",
                QuestionnaireErrors.AiInsufficientData);
        }

        if (msg.Length > MaxUserMessageChars)
        {
            return Result<AiCopilotChatResponseDto>.Fail(
                $"Message exceeds maximum length ({MaxUserMessageChars}).",
                QuestionnaireErrors.InvalidOperation);
        }

        var filter = request.AnalyticsFilter ?? new CrossSurveyAnalyticsFilterRequest();
        if (request.SurveyId.HasValue)
            filter.SurveyId = request.SurveyId;
        filter.IncludeAnswerDetails = filter.SurveyId.HasValue;

        var contextResult = await BuildSuggestContextForAiAsync(filter, cancellationToken).ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<AiCopilotChatResponseDto>.Fail(contextResult.Errors, contextResult.FailureCode);

        var historyBlock = FormatCopilotHistory(request.History);
        var user = AiPromptEngine.BuildCopilotUserPrompt(contextResult.Value!, historyBlock, msg);

        var completion = await _openAi.CompleteAsync(
            new List<(string, string)> { ("system", AiPromptEngine.MasterCopilotRole), ("user", user) },
            jsonObjectFormat: true,
            cancellationToken).ConfigureAwait(false);

        if (!completion.IsSuccess)
            return Result<AiCopilotChatResponseDto>.Fail(completion.Errors, completion.FailureCode);

        try
        {
            var raw = JsonSerializer.Deserialize<AiCopilotRawDto>(completion.Value!, CopilotDeserialize);
            if (raw == null || string.IsNullOrWhiteSpace(raw.ReplyAr) || string.IsNullOrWhiteSpace(raw.ReplyEn))
            {
                return Result<AiCopilotChatResponseDto>.Fail(
                    "AI returned an incomplete reply.",
                    QuestionnaireErrors.AiProviderError);
            }

            var cards = NormalizeInsightCards(raw.InsightCards);
            var sugAr = NormalizeStringList(raw.SuggestedPromptsAr, 5);
            var sugEn = NormalizeStringList(raw.SuggestedPromptsEn, 5);

            return Result<AiCopilotChatResponseDto>.Ok(new AiCopilotChatResponseDto
            {
                ReplyAr = raw.ReplyAr.Trim(),
                ReplyEn = raw.ReplyEn.Trim(),
                InsightCards = cards,
                SuggestedPromptsAr = sugAr,
                SuggestedPromptsEn = sugEn,
            });
        }
        catch (Exception ex)
        {
            return Result<AiCopilotChatResponseDto>.Fail(ex.Message, QuestionnaireErrors.AiProviderError);
        }
    }

    private static string FormatCopilotHistory(IReadOnlyList<AiCopilotMessageDto>? history)
    {
        if (history == null || history.Count == 0)
            return "(no prior turns)";

        var take = history.Count > MaxCopilotHistory ? history.Skip(history.Count - MaxCopilotHistory).ToList() : history.ToList();
        var sb = new StringBuilder();
        foreach (var m in take)
        {
            var role = (m.Role ?? "user").Trim().ToLowerInvariant();
            if (role != "user" && role != "assistant" && role != "system")
                role = "user";
            var content = (m.Content ?? string.Empty).Trim();
            if (content.Length == 0)
                continue;
            if (content.Length > 2000)
                content = content[..2000] + "…";
            sb.Append('[').Append(role).Append("] ").Append(content).Append('\n');
        }

        return sb.Length == 0 ? "(no prior turns)" : sb.ToString();
    }

    private static string NormalizeQuestionTypeString(string? raw)
    {
        var s = (raw ?? string.Empty).Trim();
        if (s.Length == 0)
            return nameof(QuestionType.ShortText);
        if (int.TryParse(s, out var num) && Enum.IsDefined(typeof(QuestionType), (byte)num))
            return ((QuestionType)(byte)num).ToString();

        s = s.Replace(" ", string.Empty, StringComparison.Ordinal);
        foreach (var name in Enum.GetNames<QuestionType>())
        {
            if (string.Equals(s, name, StringComparison.OrdinalIgnoreCase))
                return name;
        }

        return nameof(QuestionType.ShortText);
    }

    private sealed class AiSurveyGenRawDto
    {
        public string? TitleAr { get; set; }
        public string? TitleEn { get; set; }
        public string? DescriptionAr { get; set; }
        public string? DescriptionEn { get; set; }
        public List<AiSurveyQuestionRawDto>? Questions { get; set; }
    }

    private sealed class AiSurveyQuestionRawDto
    {
        public string? Type { get; set; }
        public string? TitleAr { get; set; }
        public string? TitleEn { get; set; }
        public bool Required { get; set; }
        public List<AiSurveyOptionRawDto>? Options { get; set; }
    }

    private sealed class AiSurveyOptionRawDto
    {
        public string? TextAr { get; set; }
        public string? TextEn { get; set; }
    }

    private sealed class AiSentimentRawDto
    {
        public string? SummaryAr { get; set; }
        public string? SummaryEn { get; set; }
        public string? OverallToneAr { get; set; }
        public string? OverallToneEn { get; set; }
        public int? PositiveCount { get; set; }
        public int? NegativeCount { get; set; }
        public int? NeutralCount { get; set; }
        public AiChartRawDto? InsightChart { get; set; }
        public List<AiInsightCardRawDto>? Cards { get; set; }
    }

    private sealed class AiCopilotRawDto
    {
        public string? ReplyAr { get; set; }
        public string? ReplyEn { get; set; }
        public List<AiInsightCardRawDto>? InsightCards { get; set; }
        public List<string>? SuggestedPromptsAr { get; set; }
        public List<string>? SuggestedPromptsEn { get; set; }
    }
}
