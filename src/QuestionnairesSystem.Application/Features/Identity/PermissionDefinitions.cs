namespace QuestionnairesSystem.Application.Features.Identity;

internal static class PermissionDefinitions
{
    internal sealed record Definition(
        string Code,
        string NameAr,
        string NameEn,
        string? DescriptionAr,
        string? DescriptionEn,
        string Module);

    internal static readonly IReadOnlyList<Definition> All =
    [
        new(PermissionCodes.UserManage, "إدارة المستخدمين", "Manage users", null, "Manage users", "Identity"),
        new(PermissionCodes.RoleManage, "إدارة الأدوار والصلاحيات", "Manage roles and permissions", null, "Manage roles and permissions", "Identity"),
        new(PermissionCodes.AuditLogView, "عرض سجل التدقيق", "View audit logs", null, "View audit logs", "Audit"),
        new(PermissionCodes.NotificationView, "عرض قوالب وسجلات الإشعارات", "View notification templates and any notification record", null, "View notification templates and any notification record", "Notifications"),
        new(PermissionCodes.NotificationManage, "إنشاء الإشعارات وإدارة القوالب", "Create notifications and manage templates", null, "Create notifications and manage templates", "Notifications"),

        // Questionnaires modules
        new(PermissionCodes.SurveyView, "عرض الاستبيانات", "View surveys", null, "View surveys", "Survey"),
        new(PermissionCodes.SurveyManage, "إدارة الاستبيانات", "Manage surveys", null, "Create, update, publish, close, and workflow actions", "Survey"),
        new(PermissionCodes.QuestionView, "عرض الأسئلة", "View questions", null, "View survey questions", "Question"),
        new(PermissionCodes.QuestionManage, "إدارة الأسئلة", "Manage questions", null, "Create, update, delete, and reorder questions", "Question"),
        new(PermissionCodes.TemplateView, "عرض القوالب", "View templates", null, "View templates", "Template"),
        new(PermissionCodes.TemplateManage, "إدارة القوالب", "Manage templates", null, "Create, update, archive, and use templates", "Template"),
        new(PermissionCodes.ParticipantView, "عرض المشاركين", "View participants", null, "View survey participants", "Participant"),
        new(PermissionCodes.ParticipantManage, "إدارة المشاركين", "Manage participants", null, "Add and manage survey participants", "Participant"),
        new(PermissionCodes.ResponseView, "عرض الردود", "View responses", null, "View survey responses", "Response"),
        new(PermissionCodes.ResponseManage, "إدارة الردود", "Manage responses", null, "Create and submit responses", "Response"),
        new(PermissionCodes.ReportView, "عرض التقارير", "View reports", null, "View analytics and reports", "Report"),
        new(PermissionCodes.ReportExport, "تصدير التقارير", "Export reports", null, "Export reports to PDF/Excel", "Report"),
        new(PermissionCodes.RecommendationView, "عرض التوصيات", "View recommendations", null, "View recommendations", "Recommendation"),
        new(PermissionCodes.RecommendationManage, "إدارة التوصيات", "Manage recommendations", null, "Create, update, and delete recommendations", "Recommendation"),
        new(PermissionCodes.ActionPlanView, "عرض خطط العمل", "View action plans", null, "View action plans and initiatives", "ActionPlan"),
        new(PermissionCodes.ActionPlanManage, "إدارة خطط العمل", "Manage action plans", null, "Manage action plans, initiatives, and progress", "ActionPlan"),
        new(PermissionCodes.SettingsView, "عرض الإعدادات", "View settings", null, "View system settings", "Settings"),
        new(PermissionCodes.SettingsManage, "إدارة الإعدادات", "Manage settings", null, "Update system settings", "Settings"),
        new(PermissionCodes.LookupView, "عرض القوائم المرجعية", "View lookups", null, "Access lookups endpoint", "Lookup"),
        new(PermissionCodes.IntegrationManage, "إدارة التكاملات", "Manage integrations", null, "Import/export integrations and webhooks", "Integration")
    ];
}
