namespace QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;

public sealed class CreateParticipantRequest
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? ExternalReference { get; set; }
}
