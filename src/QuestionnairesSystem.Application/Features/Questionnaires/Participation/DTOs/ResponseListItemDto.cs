using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;

public sealed class ResponseListItemDto
{
    public Guid Id { get; init; }
    public Guid SurveyId { get; init; }
    public Guid? ParticipantId { get; init; }

    public string? RespondentDisplayName { get; init; }

    public ResponseStatus Status { get; init; }
    public DateTime? StartedAtUtc { get; init; }
    public DateTime? SubmittedAtUtc { get; init; }
}
