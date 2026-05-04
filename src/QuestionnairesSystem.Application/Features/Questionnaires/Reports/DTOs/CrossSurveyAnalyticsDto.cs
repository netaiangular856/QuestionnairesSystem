using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;

public sealed class CrossSurveyAnalyticsFilterRequest
{
    public Guid? SurveyId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }

    /// <summary>Export language: <c>ar</c> or <c>en</c> (PDF/Excel). Query: <c>lang</c>.</summary>
    public string? Lang { get; set; }

    /// <summary>When true, loads every answer row for PDF/Excel (large). Not used by the analytics JSON API by default.</summary>
    public bool IncludeAnswerDetails { get; set; }
}

public sealed class CrossSurveyAnalyticsDto
{
    public CrossSurveyAnalyticsFilterSnapshotDto AppliedFilter { get; init; } = new();
    public CrossSurveyOverviewDto Overview { get; init; } = new();
    public IReadOnlyList<NamedCountDto> SurveyStatusDistribution { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<NamedCountDto> ResponseStatusDistribution { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<NamedCountDto> AudienceScopeDistribution { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<NamedCountDto> ParticipantStatusDistribution { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<NamedCountDto> SubmissionsByDayOfWeek { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<TimelinePointDto> SubmissionsByDay { get; init; } = Array.Empty<TimelinePointDto>();
    public IReadOnlyList<TopSurveyRowDto> TopSurveysBySubmissions { get; init; } = Array.Empty<TopSurveyRowDto>();

    public IReadOnlyList<NamedCountDto> ActionPlanStatusDistribution { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<NamedCountDto> InitiativeStatusDistribution { get; init; } = Array.Empty<NamedCountDto>();
    public IReadOnlyList<CrossSurveyActionPlanReportRowDto> ActionPlans { get; init; } = Array.Empty<CrossSurveyActionPlanReportRowDto>();
    public IReadOnlyList<CrossSurveyInitiativeReportRowDto> Initiatives { get; init; } = Array.Empty<CrossSurveyInitiativeReportRowDto>();

    /// <summary>Aggregate rating/scale values across submitted responses in scope.</summary>
    public IReadOnlyList<RatingAnalyticsDto> RatingsDistribution { get; init; } = Array.Empty<RatingAnalyticsDto>();

    /// <summary>Counts of answers grouped by question type (same scope as submitted responses).</summary>
    public IReadOnlyList<NamedCountDto> QuestionTypeAnswerTotals { get; init; } = Array.Empty<NamedCountDto>();

    /// <summary>Top tokens from ShortText/LongText answers (frequency-based, not AI).</summary>
    public IReadOnlyList<KeywordCountDto> TextAnswerKeywords { get; init; } = Array.Empty<KeywordCountDto>();

    /// <summary>One row per question-answer in submitted responses (only when <see cref="CrossSurveyAnalyticsFilterRequest.IncludeAnswerDetails"/> is set).</summary>
    public IReadOnlyList<CrossSurveyAnswerDetailRowDto> AnswerDetails { get; init; } = Array.Empty<CrossSurveyAnswerDetailRowDto>();
}

/// <summary>Flat row for full answer export (no response/survey IDs in report output).</summary>
public sealed class CrossSurveyAnswerDetailRowDto
{
    public string SurveyTitleAr { get; init; } = string.Empty;
    public string SurveyTitleEn { get; init; } = string.Empty;
    public DateTime? SubmittedAtUtc { get; init; }
    public string? RespondentDisplayName { get; init; }
    public string QuestionTitleAr { get; init; } = string.Empty;
    public string QuestionTitleEn { get; init; } = string.Empty;
    public string QuestionTypeKey { get; init; } = string.Empty;
    public string AnswerTextAr { get; init; } = string.Empty;
    public string AnswerTextEn { get; init; } = string.Empty;
}

public sealed class CrossSurveyAnalyticsFilterSnapshotDto
{
    public Guid? SurveyId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }

    /// <summary>Set when a single survey is filtered — for display names (no IDs in reports).</summary>
    public string? SurveyTitleAr { get; init; }

    public string? SurveyTitleEn { get; init; }
}

public sealed class CrossSurveyOverviewDto
{
    public int SurveysInScope { get; init; }
    public int PublishedSurveys { get; init; }
    public int SubmittedResponsesInPeriod { get; init; }
    public int SurveysWithSubmissionsInPeriod { get; init; }
    public int InvitedParticipantsInScope { get; init; }
    public int InProgressResponsesOpen { get; init; }
    public double SubmissionsPerDayInPeriod { get; init; }
    public int TotalQuestionsInScope { get; init; }
    public int CompletedParticipantsInScope { get; init; }
    public int DeclinedParticipantsInScope { get; init; }
    public double AverageMinutesToSubmitInPeriod { get; init; }

    /// <summary>Action plans matching report scope (survey + created date filter).</summary>
    public int ActionPlansInScope { get; init; }

    /// <summary>Initiatives under action plans in scope (survey + created date filter).</summary>
    public int InitiativesInScope { get; init; }
}

public sealed class CrossSurveyActionPlanReportRowDto
{
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? LinkedSurveyTitleAr { get; init; }
    public string? LinkedSurveyTitleEn { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

public sealed class CrossSurveyInitiativeReportRowDto
{
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string ActionPlanTitleAr { get; init; } = string.Empty;
    public string ActionPlanTitleEn { get; init; } = string.Empty;
    public string? LinkedSurveyTitleAr { get; init; }
    public string? LinkedSurveyTitleEn { get; init; }
    public DateTime? TargetDateUtc { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

public sealed class NamedCountDto
{
    /// <summary>Backend enum name e.g. Draft, Submitted.</summary>
    public string Key { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class TimelinePointDto
{
    public string Date { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class TopSurveyRowDto
{
    public Guid SurveyId { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int SubmissionsInPeriod { get; init; }
}
