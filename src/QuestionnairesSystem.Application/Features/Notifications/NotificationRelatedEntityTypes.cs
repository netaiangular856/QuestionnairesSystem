namespace QuestionnairesSystem.Application.Features.Notifications;

/// <summary>Values stored in <see cref="Domain.Notifications.InboxNotification.RelatedEntityType"/> for deep links / filtering.</summary>
public static class NotificationRelatedEntityTypes
{
    public const string Survey = "Survey";
    public const string SurveyTemplate = "SurveyTemplate";
    public const string Recommendation = "Recommendation";
    public const string ActionPlan = "ActionPlan";
    public const string Initiative = "Initiative";
    public const string SurveyResponse = "SurveyResponse";
    public const string SurveyParticipant = "SurveyParticipant";
}
