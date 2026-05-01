using QuestionnairesSystem.Domain.Common;

namespace QuestionnairesSystem.Domain.Responses;

public sealed class QuestionAnswer : AuditableDomainEntity
{
    public Guid ResponseId { get; set; }
    public Guid QuestionId { get; set; }
    /// <summary>JSON value: text, selected option ids, number, etc.</summary>
    public string ValueJson { get; set; } = "{}";

    public SurveyResponse Response { get; set; } = null!;
    public Surveys.Question Question { get; set; } = null!;
}
