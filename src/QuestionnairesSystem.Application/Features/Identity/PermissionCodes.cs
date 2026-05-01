namespace QuestionnairesSystem.Application.Features.Identity;

/// <summary>Normalized permission codes for policies and seeding.</summary>
public static class PermissionCodes
{
    public const string UserManage = "USER_MANAGE";

    public const string RoleManage = "ROLE_MANAGE";

    public const string AuditLogView = "AUDIT_LOG_VIEW";

    public const string NotificationView = "NOTIFICATION_VIEW";

    public const string NotificationManage = "NOTIFICATION_MANAGE";

    // Questionnaires domain modules (v1 roadmap)
    public const string SurveyView = "SURVEY_VIEW";
    public const string SurveyManage = "SURVEY_MANAGE";
    public const string QuestionView = "QUESTION_VIEW";
    public const string QuestionManage = "QUESTION_MANAGE";
    public const string TemplateView = "TEMPLATE_VIEW";
    public const string TemplateManage = "TEMPLATE_MANAGE";
    public const string ParticipantView = "PARTICIPANT_VIEW";
    public const string ParticipantManage = "PARTICIPANT_MANAGE";
    public const string ResponseView = "RESPONSE_VIEW";
    public const string ResponseManage = "RESPONSE_MANAGE";
    public const string ReportView = "REPORT_VIEW";
    public const string ReportExport = "REPORT_EXPORT";
    public const string RecommendationView = "RECOMMENDATION_VIEW";
    public const string RecommendationManage = "RECOMMENDATION_MANAGE";
    public const string ActionPlanView = "ACTION_PLAN_VIEW";
    public const string ActionPlanManage = "ACTION_PLAN_MANAGE";
    public const string SettingsView = "SETTINGS_VIEW";
    public const string SettingsManage = "SETTINGS_MANAGE";
    public const string LookupView = "LOOKUP_VIEW";
    public const string IntegrationManage = "INTEGRATION_MANAGE";
}
