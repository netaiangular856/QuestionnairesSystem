using QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;

namespace QuestionnairesSystem.Application.Features.Ai.DTOs;

public sealed class TranslateRichTextRequest
{
    /// <summary>Fragment or HTML from rich-text editors.</summary>
    public string Html { get; set; } = string.Empty;

    /// <summary>Source locale: <c>ar</c> or <c>en</c>.</summary>
    public string SourceLang { get; set; } = "ar";

    /// <summary>Target locale: <c>ar</c> or <c>en</c>.</summary>
    public string TargetLang { get; set; } = "en";
}

public sealed class TranslateRichTextResponse
{
    public string Html { get; set; } = string.Empty;
}

public sealed class AiSuggestFromSurveyRequest
{
    public Guid? SurveyId { get; set; }
}

public sealed class AiSuggestRecommendationDraftDto
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }

    /// <summary>Use 3 (low), 5 (medium), or 8 (high) to match the Angular tiers.</summary>
    public int Priority { get; set; } = 5;
}

public sealed class AiSuggestActionPlanDraftDto
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
}

public sealed class AiAnalyzeReportsRequest
{
    /// <summary>Same filters as cross-survey analytics.</summary>
    public CrossSurveyAnalyticsFilterRequest Filter { get; set; } = new();
}

public sealed class AiAnalyzeReportsResponseDto
{
    /// <summary>Short executive summary in Arabic (safe HTML: p, ul, li, strong).</summary>
    public string SummaryAr { get; set; } = string.Empty;

    /// <summary>Same summary in English.</summary>
    public string SummaryEn { get; set; } = string.Empty;

    public IReadOnlyList<AiChartSuggestionDto> Charts { get; init; } = Array.Empty<AiChartSuggestionDto>();

    /// <summary>Optional KPI chips (numeric snapshot).</summary>
    public IReadOnlyList<AiKpiChipDto> Kpis { get; init; } = Array.Empty<AiKpiChipDto>();

    /// <summary>Smart insight cards (risk, opportunity, sentiment, etc.).</summary>
    public IReadOnlyList<AiInsightCardDto> InsightCards { get; init; } = Array.Empty<AiInsightCardDto>();

    public IReadOnlyList<string> RecommendationsAr { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RecommendationsEn { get; init; } = Array.Empty<string>();

    /// <summary>Very short executive headline box (HTML allowed).</summary>
    public string ExecutiveBoxAr { get; set; } = string.Empty;

    public string ExecutiveBoxEn { get; set; } = string.Empty;

    public IReadOnlyList<string> ActionPlanStepsAr { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ActionPlanStepsEn { get; init; } = Array.Empty<string>();
}

public sealed class AiChartSuggestionDto
{
    /// <summary>Arabic chart title.</summary>
    public string TitleAr { get; set; } = string.Empty;

    /// <summary>English chart title.</summary>
    public string TitleEn { get; set; } = string.Empty;

    /// <summary>Legacy single title (usually English); prefer <see cref="TitleAr"/>/<see cref="TitleEn"/>.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary><c>bar</c>, <c>line</c>, or <c>doughnut</c>.</summary>
    public string Kind { get; set; } = "bar";

    public IReadOnlyList<string> Labels { get; init; } = Array.Empty<string>();
    public IReadOnlyList<double> Values { get; init; } = Array.Empty<double>();
}

/// <summary>Compact KPI line for dashboards / AI panels.</summary>
public sealed class AiKpiChipDto
{
    public string LabelAr { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public string ValueText { get; set; } = string.Empty;
    public string? HintAr { get; set; }
    public string? HintEn { get; set; }
}

/// <summary>Structured insight for enterprise AI cards (risk, sentiment, recommendation, etc.).</summary>
public sealed class AiInsightCardDto
{
    /// <summary>
    /// One of: <c>risk_detected</c>, <c>low_satisfaction</c>, <c>improvement_opportunity</c>, <c>executive_insight</c>,
    /// <c>recommended_action</c>, <c>sentiment_summary</c>.
    /// </summary>
    public string Kind { get; set; } = "executive_insight";

    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string BodyAr { get; set; } = string.Empty;
    public string BodyEn { get; set; } = string.Empty;

    /// <summary>Optional: <c>low</c>, <c>medium</c>, <c>high</c>.</summary>
    public string? Severity { get; set; }
}

public sealed class AiGenerateSurveyRequest
{
    /// <summary>High-level goal in Arabic (e.g. قياس رضا الموظفين عن الخدمات الداخلية).</summary>
    public string? BriefAr { get; set; }

    /// <summary>Same goal in English (optional if BriefAr provided).</summary>
    public string? BriefEn { get; set; }

    /// <summary>Target number of questions (clamped server-side).</summary>
    public int? MaxQuestions { get; set; }
}

public sealed class AiGeneratedSurveyDraftDto
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public IReadOnlyList<AiGeneratedQuestionDraftDto> Questions { get; init; } = Array.Empty<AiGeneratedQuestionDraftDto>();
}

public sealed class AiGeneratedQuestionDraftDto
{
    /// <summary>One of ShortText, LongText, SingleChoice, MultipleChoice, Rating, Scale, YesNo, Date, Number.</summary>
    public string Type { get; set; } = "ShortText";

    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public bool Required { get; set; }
    public IReadOnlyList<AiGeneratedOptionDraftDto>? Options { get; init; }
}

public sealed class AiGeneratedOptionDraftDto
{
    public string TextAr { get; set; } = string.Empty;
    public string TextEn { get; set; } = string.Empty;
}

/// <summary>
/// One-click: AI reads recent recommendations + survey titles, proposes questions, server persists a new draft survey.
/// </summary>
public sealed class AiAutoSurveyFromRecommendationsRequest
{
    /// <summary>Clamped 3–20 (default 12).</summary>
    public int? MaxQuestions { get; set; }

    /// <summary>How many recommendation rows to pass into context (default 40, max 80).</summary>
    public int? MaxRecommendations { get; set; }

    /// <summary>How many recent surveys to summarize by title (default 25, max 60).</summary>
    public int? RecentSurveyCount { get; set; }
}

public sealed class AiAutoCreateSurveyResponseDto
{
    public Guid SurveyId { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
}

public sealed class AiSentimentAnalysisRequest
{
    /// <summary>
    /// Optional. When omitted, sentiment is computed across surveys in the provided <see cref="AnalyticsFilter"/> window.
    /// </summary>
    public Guid? SurveyId { get; set; }

    /// <summary>Optional analytics window (same semantics as reports).</summary>
    public CrossSurveyAnalyticsFilterRequest? AnalyticsFilter { get; set; }

    /// <summary>
    /// When no explicit date range is provided, the server will default to the last N days (applies mainly to cross-survey sentiment).
    /// </summary>
    public int? DefaultWindowDays { get; set; }
}

/// <summary>Counts of text excerpts classified as positive / negative / neutral (same window as the analysis).</summary>
public sealed class AiSentimentMixDto
{
    public int Positive { get; init; }
    public int Negative { get; init; }
    public int Neutral { get; init; }
}

public sealed class AiSentimentAnalysisResponseDto
{
    public string SummaryAr { get; set; } = string.Empty;
    public string SummaryEn { get; set; } = string.Empty;
    public string? OverallToneAr { get; set; }
    public string? OverallToneEn { get; set; }

    /// <summary>When present, UI may render a doughnut chart for polarity mix.</summary>
    public AiSentimentMixDto? SentimentMix { get; init; }

    /// <summary>
    /// Optional second chart (bar or line) from the same excerpt analysis, e.g. tone by question title or theme (not doughnut).
    /// </summary>
    public AiChartSuggestionDto? InsightChart { get; init; }

    public IReadOnlyList<AiInsightCardDto> Cards { get; init; } = Array.Empty<AiInsightCardDto>();
}

public sealed class AiCopilotMessageDto
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public sealed class AiCopilotChatRequest
{
    public Guid? SurveyId { get; set; }

    /// <summary>Optional analytics window (same semantics as reports).</summary>
    public CrossSurveyAnalyticsFilterRequest? AnalyticsFilter { get; set; }

    /// <summary>Recent turns (newest last). Server caps length.</summary>
    public IReadOnlyList<AiCopilotMessageDto>? History { get; set; }

    public string UserMessage { get; set; } = string.Empty;
}

public sealed class AiCopilotChatResponseDto
{
    public string ReplyAr { get; set; } = string.Empty;
    public string ReplyEn { get; set; } = string.Empty;
    public IReadOnlyList<AiInsightCardDto> InsightCards { get; init; } = Array.Empty<AiInsightCardDto>();
    public IReadOnlyList<string> SuggestedPromptsAr { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SuggestedPromptsEn { get; init; } = Array.Empty<string>();
}
