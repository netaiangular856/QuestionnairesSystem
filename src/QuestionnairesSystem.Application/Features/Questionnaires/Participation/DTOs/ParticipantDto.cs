using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Participation.DTOs;

public sealed class ParticipantDto
{
    public Guid Id { get; init; }
    public Guid SurveyId { get; init; }
    public Guid? UserId { get; init; }

    /// <summary>Linked system user — for display when <see cref="UserId"/> is set.</summary>
    public string? UserDisplayName { get; init; }

    public string? Email { get; init; }
    public string? ExternalReference { get; init; }
    public ParticipantStatus Status { get; init; }
    public DateTime? InvitedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
}
