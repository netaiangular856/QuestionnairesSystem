using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Domain.Identity;

namespace QuestionnairesSystem.Domain.Responses;

public sealed class SurveyResponse : AuditableDomainEntity
{
    public Guid SurveyId { get; set; }
    public Guid? ParticipantId { get; set; }
    public Guid? RespondentUserId { get; set; }
    public ResponseStatus Status { get; set; } = ResponseStatus.InProgress;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }

    public Surveys.Survey Survey { get; set; } = null!;
    public Participants.SurveyParticipant? Participant { get; set; }
    public User? RespondentUser { get; set; }
    public ICollection<QuestionAnswer> Answers { get; set; } = new List<QuestionAnswer>();
}
