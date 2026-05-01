namespace QuestionnairesSystem.Application.Features.Questionnaires.Questions.DTOs;

public sealed class ReorderQuestionsRequest
{
    public IReadOnlyList<Guid> QuestionIds { get; set; } = Array.Empty<Guid>();
}
