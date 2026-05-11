import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { PermissionCodes } from './shared/models/permission-codes';

export const routes: Routes = [
  {
    path: 'home',
    loadComponent: () => import('./features/landing/landing-page.component').then((m) => m.LandingPageComponent),
  },
  {
    path: 'portal',
    loadComponent: () => import('./features/portal/portal-public-page.component').then((m) => m.PortalPublicPageComponent),
  },
  {
    path: 'portal/s/:code/fill',
    loadComponent: () =>
      import('./features/portal/public-survey-fill-page.component').then((m) => m.PublicSurveyFillPageComponent),
  },
  {
    path: 'portal/s/:code',
    loadComponent: () =>
      import('./features/portal/public-survey-intro-page.component').then((m) => m.PublicSurveyIntroPageComponent),
  },
  { path: '', pathMatch: 'full', redirectTo: 'home' },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login-page.component').then((m) => m.LoginPageComponent),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register-page.component').then((m) => m.RegisterPageComponent),
  },
  {
    path: '403',
    canActivate: [authGuard],
    loadComponent: () => import('./features/forbidden/forbidden-page.component').then((m) => m.ForbiddenPageComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/app-shell.component').then((m) => m.AppShellComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard-page.component').then((m) => m.DashboardPageComponent),
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/profile/profile-page.component').then((m) => m.ProfilePageComponent),
      },
      {
        path: 'change-password',
        loadComponent: () =>
          import('./features/profile/change-password-page.component').then((m) => m.ChangePasswordPageComponent),
      },
      {
        path: 'users',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.UserManage] },
        loadComponent: () => import('./features/users/users-page.component').then((m) => m.UsersPageComponent),
      },
      {
        path: 'roles',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.RoleManage] },
        loadComponent: () => import('./features/roles/roles-page.component').then((m) => m.RolesPageComponent),
      },
      {
        path: 'audit-logs',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.AuditLogView] },
        loadComponent: () =>
          import('./features/audit-logs/audit-logs-page.component').then((m) => m.AuditLogsPageComponent),
      },
      {
        path: 'notifications',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.NotificationView] },
        loadComponent: () =>
          import('./features/notifications/notifications-page.component').then((m) => m.NotificationsPageComponent),
      },
      {
        path: 'surveys/new',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.SurveyManage] },
        loadComponent: () =>
          import('./features/questionnaires/surveys/survey-create-wizard-page/survey-create-wizard-page.component').then(
            (m) => m.SurveyCreateWizardPageComponent,
          ),
      },
      {
        path: 'employees',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.EmployeeView] },
        loadComponent: () =>
          import('./features/employees/employees-list.component').then((m) => m.EmployeesListComponent),
      },
      {
        path: 'departments',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.DepartmentView] },
        loadComponent: () =>
          import('./features/departments/departments-list.component').then((m) => m.DepartmentsListComponent),
      },
      {
        path: 'partners',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.PartnerView] },
        loadComponent: () =>
          import('./features/partners/partners-list.component').then((m) => m.PartnersListComponent),
      },
      {
        path: 'data-import',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.DataBulkImport] },
        loadComponent: () =>
          import('./features/data-import/data-import-page.component').then((m) => m.DataImportPageComponent),
      },
      {
        path: 'surveys-approval',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.SurveyApprovalView] },
        loadComponent: () =>
          import('./features/questionnaires/surveys/survey-approval-page/survey-approval-page.component').then((m) => m.SurveyApprovalPageComponent),
      },
      {
        path: 'available-surveys',
        loadComponent: () =>
          import('./features/questionnaires/surveys/available-surveys-page/available-surveys-page.component').then((m) => m.AvailableSurveysPageComponent),
      },
      {
        path: 'surveys/:surveyId/fill',
        loadComponent: () =>
          import('./features/questionnaires/surveys/survey-fill-page/survey-fill-page.component').then((m) => m.SurveyFillPageComponent),
      },
      {
        path: 'surveys/:surveyId/participants/:participantId',
        loadComponent: () =>
          import('./features/questionnaires/surveys/survey-fill-page/survey-fill-page.component').then((m) => m.SurveyFillPageComponent),
      },
      {
        path: 'surveys/:surveyId/responses/:responseId',
        loadComponent: () =>
          import('./features/questionnaires/surveys/survey-fill-page/survey-fill-page.component').then((m) => m.SurveyFillPageComponent),
      },
      {
        path: 'surveys',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.SurveyView] },
        loadComponent: () =>
          import('./features/questionnaires/surveys/surveys-page/surveys-page.component').then((m) => m.SurveysPageComponent),
      },
      {
        path: 'surveys/:surveyId/responses',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ResponseView] },
        loadComponent: () =>
          import('./features/questionnaires/surveys/survey-responses-page/survey-responses-page.component').then((m) => m.SurveyResponsesPageComponent),
      },
      {
        path: 'surveys/:surveyId/participants/:participantId',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ParticipantView] },
        loadComponent: () =>
          import('./features/questionnaires/surveys/survey-participant-detail-page/survey-participant-detail-page.component').then(
            (m) => m.SurveyParticipantDetailPageComponent,
          ),
      },
      {
        path: 'surveys/:surveyId/participants',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ParticipantView] },
        loadComponent: () =>
          import('./features/questionnaires/surveys/survey-participants-page/survey-participants-page.component').then((m) => m.SurveyParticipantsPageComponent),
      },
      {
        path: 'surveys/:surveyId/question-analytics',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ReportView] },
        loadComponent: () =>
          import('./features/questionnaires/surveys/survey-question-analytics-page/survey-question-analytics-page.component').then(
            (m) => m.SurveyQuestionAnalyticsPageComponent,
          ),
      },
      {
        path: 'surveys/:surveyId/edit',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.SurveyManage] },
        loadComponent: () =>
          import('./features/questionnaires/surveys/survey-editor-page/survey-editor-page.component').then((m) => m.SurveyEditorPageComponent),
      },
      {
        path: 'surveys/:surveyId',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.SurveyView] },
        loadComponent: () =>
          import('./features/questionnaires/surveys/survey-detail-page/survey-detail-page.component').then((m) => m.SurveyDetailPageComponent),
      },
      {
        path: 'templates/new',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.TemplateManage] },
        loadComponent: () =>
          import('./features/questionnaires/templates/template-editor-page/template-editor-page.component').then((m) => m.TemplateEditorPageComponent),
      },
      {
        path: 'templates/:templateId/edit',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.TemplateManage] },
        loadComponent: () =>
          import('./features/questionnaires/templates/template-editor-page/template-editor-page.component').then((m) => m.TemplateEditorPageComponent),
      },
      {
        path: 'templates/:templateId',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.TemplateView] },
        loadComponent: () =>
          import('./features/questionnaires/templates/template-detail-page/template-detail-page.component').then((m) => m.TemplateDetailPageComponent),
      },
      {
        path: 'templates',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.TemplateView] },
        loadComponent: () =>
          import('./features/questionnaires/templates/templates-page/templates-page.component').then((m) => m.TemplatesPageComponent),
      },
      {
        path: 'recommendations',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.RecommendationView] },
        loadComponent: () =>
          import('./features/questionnaires/recommendations/recommendations-page/recommendations-page.component').then((m) => m.RecommendationsPageComponent),
      },
      {
        path: 'action-plans/new',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ActionPlanManage] },
        loadComponent: () =>
          import('./features/questionnaires/action-plans/action-plan-create-page/action-plan-create-page.component').then((m) => m.ActionPlanCreatePageComponent),
      },
      {
        path: 'action-plans/:planId',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ActionPlanView] },
        loadComponent: () =>
          import('./features/questionnaires/action-plans/action-plan-detail-page/action-plan-detail-page.component').then((m) => m.ActionPlanDetailPageComponent),
      },
      {
        path: 'action-plans',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ActionPlanView] },
        loadComponent: () =>
          import('./features/questionnaires/action-plans/action-plans-page/action-plans-page.component').then((m) => m.ActionPlansPageComponent),
      },
      {
        path: 'initiatives',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ActionPlanView] },
        loadComponent: () =>
          import('./features/questionnaires/initiatives/initiatives-page/initiatives-page.component').then((m) => m.InitiativesPageComponent),
      },
      {
        path: 'initiatives/:initiativeId',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ActionPlanView] },
        loadComponent: () =>
          import('./features/questionnaires/initiatives/initiative-detail-page/initiative-detail-page.component').then((m) => m.InitiativeDetailPageComponent),
      },
      {
        path: 'reports',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ReportView] },
        loadComponent: () =>
          import('./features/questionnaires/analytics/reports-page/reports-page.component').then((m) => m.ReportsPageComponent),
      },
      {
        path: 'survey-analysis',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ReportView] },
        loadComponent: () =>
          import('./features/questionnaires/analytics/survey-analysis-page/survey-analysis-page.component').then((m) => m.SurveyAnalysisPageComponent),
      },
      {
        path: 'impact-measurement',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ReportView] },
        loadComponent: () =>
          import('./features/questionnaires/analytics/impact-measurement-page/impact-measurement-page.component').then((m) => m.ImpactMeasurementPageComponent),
      },
      {
        path: 'ai-analysis',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ReportView] },
        loadComponent: () =>
          import('./features/questionnaires/analytics/ai-analysis-page/ai-analysis-page.component').then((m) => m.AiAnalysisPageComponent),
      },
      {
        path: 'ai-scope',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ReportView] },
        loadComponent: () =>
          import('./features/questionnaires/analytics/ai-scope-actions-page/ai-scope-actions-page.component').then(
            (m) => m.AiScopeActionsPageComponent
          ),
      },
      {
        path: 'athar-ai',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
      { path: 'survey-analytics-export', redirectTo: 'reports', pathMatch: 'full' },
    ],
  },
  { path: '**', redirectTo: 'home' },
];
