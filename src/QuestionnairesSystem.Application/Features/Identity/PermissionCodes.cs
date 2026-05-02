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
    /// <summary>عرض قائمة الاستبيانات في انتظار الاعتماد.</summary>
    public const string SurveyApprovalView = "SURVEY_APPROVAL_VIEW";
    /// <summary>اعتماد أو رفض الاستبيانات المعلقة.</summary>
    public const string SurveyApprove = "SURVEY_APPROVE";
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
    public const string EmployeeView = "EMPLOYEE_VIEW";
    public const string EmployeeManage = "EMPLOYEE_MANAGE";

    public const string DepartmentView = "DEPARTMENT_VIEW";
    public const string DepartmentManage = "DEPARTMENT_MANAGE";

    public const string PartnerView = "PARTNER_VIEW";
    public const string PartnerManage = "PARTNER_MANAGE";
    public const string SettingsView = "SETTINGS_VIEW";
    public const string SettingsManage = "SETTINGS_MANAGE";
    public const string LookupView = "LOOKUP_VIEW";
    public const string IntegrationManage = "INTEGRATION_MANAGE";
}
