namespace QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;

public sealed class ParticipantDetailDto
{
    public ParticipantDto Participant { get; init; } = null!;

    public ResponseDetailDto? Response { get; init; }
}
