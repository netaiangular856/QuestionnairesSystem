namespace QuestionnairesSystem.Domain.Enums;

public enum SurveyStatus : byte
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Published = 4,
    Closed = 5,
    Rejected = 6,
}
