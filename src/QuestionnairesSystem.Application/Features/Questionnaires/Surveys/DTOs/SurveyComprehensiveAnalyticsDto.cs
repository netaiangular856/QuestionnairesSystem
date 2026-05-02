namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

public sealed class SurveyComprehensiveAnalyticsDto
{
    public Guid SurveyId { get; init; }
    public string SurveyTitle { get; init; } = string.Empty;
    public SurveyOverviewAnalytics Overview { get; init; } = new();
    public IReadOnlyList<ResponseTimelineAnalytics> ResponseTimeline { get; init; } = Array.Empty<ResponseTimelineAnalytics>();
    public IReadOnlyList<QuestionAnalyticsDto> Questions { get; init; } = Array.Empty<QuestionAnalyticsDto>();
    public IReadOnlyList<CategoryAnalyticsDto> Categories { get; init; } = Array.Empty<CategoryAnalyticsDto>();
    public IReadOnlyList<RatingAnalyticsDto> Ratings { get; init; } = Array.Empty<RatingAnalyticsDto>();
}

public sealed class SurveyOverviewAnalytics
{
    public int TotalParticipants { get; init; }
    public int SubmittedResponses { get; init; }
    public int InProgressResponses { get; init; }
    public double CompletionRate { get; init; }
    public int TotalQuestions { get; init; }
}

public sealed class ResponseTimelineAnalytics
{
    public string Date { get; init; } = string.Empty;
    public int ResponseCount { get; init; }
}

public sealed class QuestionAnalyticsDto
{
    public Guid QuestionId { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public string QuestionType { get; init; } = string.Empty;
    public int TotalAnswers { get; init; }
    public IReadOnlyList<AnswerDistributionDto> AnswerDistribution { get; init; } = Array.Empty<AnswerDistributionDto>();
    public double? AverageRating { get; init; }
    public double? MinRating { get; init; }
    public double? MaxRating { get; init; }
}

public sealed class AnswerDistributionDto
{
    public string OptionText { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Percentage { get; init; }
}

public sealed class CategoryAnalyticsDto
{
    public string CategoryName { get; init; } = string.Empty;
    public int ResponseCount { get; init; }
    public double Percentage { get; init; }
}

public sealed class RatingAnalyticsDto
{
    public int Rating { get; init; }
    public int Count { get; init; }
    public double Percentage { get; init; }
}
