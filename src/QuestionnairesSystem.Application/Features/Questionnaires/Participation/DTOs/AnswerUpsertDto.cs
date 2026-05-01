namespace QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;

public sealed class AnswerUpsertDto
{
    public Guid QuestionId { get; set; }
    public string ValueJson { get; set; } = "{}";
}
