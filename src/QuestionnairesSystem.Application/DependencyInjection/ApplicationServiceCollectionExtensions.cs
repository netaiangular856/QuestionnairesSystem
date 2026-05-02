using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using QuestionnairesSystem.Application.Features.AuditLogs.Interfaces;
using QuestionnairesSystem.Application.Features.AuditLogs.Services;
using QuestionnairesSystem.Application.Features.Identity.Interfaces;
using QuestionnairesSystem.Application.Features.Identity.Services;
using QuestionnairesSystem.Application.Features.Identity.Validators;
using QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.Services;
using QuestionnairesSystem.Application.Features.Questionnaires.Participation.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Participation.Services;
using QuestionnairesSystem.Application.Features.Questionnaires.Questions.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Questions.Services;
using QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.Services;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.Services;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Services;
using QuestionnairesSystem.Application.Features.Questionnaires.Templates.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Templates.Services;
using QuestionnairesSystem.Application.Features.Notifications.Interfaces;
using QuestionnairesSystem.Application.Features.Notifications.Services;
using QuestionnairesSystem.Application.Features.Organizations.Departments.Interfaces;
using QuestionnairesSystem.Application.Features.Organizations.Departments.Services;
using QuestionnairesSystem.Application.Features.Organizations.Employees.Interfaces;
using QuestionnairesSystem.Application.Features.Organizations.Employees.Services;
using QuestionnairesSystem.Application.Features.Partners.Interfaces;
using QuestionnairesSystem.Application.Features.Partners.Services;

namespace QuestionnairesSystem.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IIdentityDatabaseSeeder, IdentityDatabaseSeeder>();

        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<ISurveyService, SurveyService>();
        services.AddScoped<ISurveyTemplateService, SurveyTemplateService>();
        services.AddScoped<IQuestionnaireQuestionService, QuestionnaireQuestionService>();
        services.AddScoped<IParticipantResponseService, ParticipantResponseService>();
        services.AddScoped<IQuestionnaireReportService, QuestionnaireReportService>();
        services.AddScoped<IRecommendationCrudService, RecommendationCrudService>();
        services.AddScoped<IActionPlanCrudService, ActionPlanCrudService>();

        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IPartnerService, PartnerService>();

        return services;
    }
}
