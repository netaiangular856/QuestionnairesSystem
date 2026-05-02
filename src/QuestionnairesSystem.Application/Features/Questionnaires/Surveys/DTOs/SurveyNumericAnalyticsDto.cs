using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

public sealed class SurveyNumericAnalyticsDto
{
    public Guid SurveyId { get; init; }
    public IReadOnlyList<NumericQuestionStatDto> Questions { get; init; } = Array.Empty<NumericQuestionStatDto>();
}

public sealed class NumericQuestionStatDto
{
    public Guid QuestionId { get; init; }
    public string TitleAr { get; init; } = string.Empty;
    public string TitleEn { get; init; } = string.Empty;
    public QuestionType Type { get; init; }
    public double? Average { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public int AnswerCount { get; init; }
}
