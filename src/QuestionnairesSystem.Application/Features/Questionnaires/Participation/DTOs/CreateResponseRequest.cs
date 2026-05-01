namespace QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;

public sealed class CreateResponseRequest
{
    public Guid? ParticipantId { get; set; }
    public Guid? RespondentUserId { get; set; }
    public IReadOnlyList<AnswerUpsertDto>? Answers { get; set; }
}
