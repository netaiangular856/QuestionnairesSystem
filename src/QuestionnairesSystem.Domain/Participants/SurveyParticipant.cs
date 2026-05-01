using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Domain.Identity;

namespace QuestionnairesSystem.Domain.Participants;

public sealed class SurveyParticipant : AuditableDomainEntity
{
    public Guid SurveyId { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? ExternalReference { get; set; }
    public ParticipantStatus Status { get; set; } = ParticipantStatus.Invited;
    public DateTime? InvitedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public Surveys.Survey Survey { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<Responses.SurveyResponse> Responses { get; set; } = new List<Responses.SurveyResponse>();
}
