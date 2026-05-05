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
