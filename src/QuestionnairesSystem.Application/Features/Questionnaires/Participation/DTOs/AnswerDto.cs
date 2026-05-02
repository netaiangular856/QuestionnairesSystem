namespace QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;

public sealed class AnswerDto
{
    public Guid QuestionId { get; init; }
    public string? QuestionTitleAr { get; init; }
    public string? QuestionTitleEn { get; init; }
    public string ValueJson { get; init; } = "{}";
}
