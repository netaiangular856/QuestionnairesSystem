namespace QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

public sealed class QuestionAnalyticsItemDto
{
    public Guid QuestionId { get; init; }
    public string TitleEn { get; init; } = string.Empty;
    public int AnswerCount { get; init; }
}
