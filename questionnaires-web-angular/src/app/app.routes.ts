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
          import('./features/questionnaires/survey-create-wizard-page.component').then(
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
        path: 'surveys-approval',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.SurveyApprovalView] },
        loadComponent: () =>
          import('./features/questionnaires/survey-approval-page.component').then((m) => m.SurveyApprovalPageComponent),
      },
      {
        path: 'available-surveys',
        loadComponent: () =>
          import('./features/questionnaires/available-surveys-page.component').then((m) => m.AvailableSurveysPageComponent),
      },
      {
        path: 'surveys/:surveyId/fill',
        loadComponent: () =>
          import('./features/questionnaires/survey-fill-page.component').then((m) => m.SurveyFillPageComponent),
      },
      {
        path: 'surveys/:surveyId/participants/:participantId',
        loadComponent: () =>
          import('./features/questionnaires/survey-fill-page.component').then((m) => m.SurveyFillPageComponent),
      },
      {
        path: 'surveys/:surveyId/responses/:responseId',
        loadComponent: () =>
          import('./features/questionnaires/survey-fill-page.component').then((m) => m.SurveyFillPageComponent),
      },
      {
        path: 'surveys',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.SurveyView] },
        loadComponent: () =>
          import('./features/questionnaires/surveys-page.component').then((m) => m.SurveysPageComponent),
      },
      {
        path: 'surveys/:surveyId/responses',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ResponseView] },
        loadComponent: () =>
          import('./features/questionnaires/survey-responses-page.component').then((m) => m.SurveyResponsesPageComponent),
      },
      {
        path: 'surveys/:surveyId/participants/:participantId',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ParticipantView] },
        loadComponent: () =>
          import('./features/questionnaires/survey-participant-detail-page.component').then(
            (m) => m.SurveyParticipantDetailPageComponent,
          ),
      },
      {
        path: 'surveys/:surveyId/participants',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ParticipantView] },
        loadComponent: () =>
          import('./features/questionnaires/survey-participants-page.component').then((m) => m.SurveyParticipantsPageComponent),
      },
      {
        path: 'surveys/:surveyId/question-analytics',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ReportView] },
        loadComponent: () =>
          import('./features/questionnaires/survey-question-analytics-page.component').then(
            (m) => m.SurveyQuestionAnalyticsPageComponent,
          ),
      },
      {
        path: 'surveys/:surveyId/edit',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.SurveyManage] },
        loadComponent: () =>
          import('./features/questionnaires/survey-editor-page.component').then((m) => m.SurveyEditorPageComponent),
      },
      {
        path: 'surveys/:surveyId',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.SurveyView] },
        loadComponent: () =>
          import('./features/questionnaires/survey-detail-page.component').then((m) => m.SurveyDetailPageComponent),
      },
      {
        path: 'templates/new',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.TemplateManage] },
        loadComponent: () =>
          import('./features/questionnaires/template-editor-page.component').then((m) => m.TemplateEditorPageComponent),
      },
      {
        path: 'templates/:templateId/edit',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.TemplateManage] },
        loadComponent: () =>
          import('./features/questionnaires/template-editor-page.component').then((m) => m.TemplateEditorPageComponent),
      },
      {
        path: 'templates/:templateId',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.TemplateView] },
        loadComponent: () =>
          import('./features/questionnaires/template-detail-page.component').then((m) => m.TemplateDetailPageComponent),
      },
      {
        path: 'templates',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.TemplateView] },
        loadComponent: () =>
          import('./features/questionnaires/templates-page.component').then((m) => m.TemplatesPageComponent),
      },
      {
        path: 'recommendations',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.RecommendationView] },
        loadComponent: () =>
          import('./features/questionnaires/recommendations-page.component').then((m) => m.RecommendationsPageComponent),
      },
      {
        path: 'action-plans',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ActionPlanView] },
        loadComponent: () =>
          import('./features/questionnaires/action-plans-page.component').then((m) => m.ActionPlansPageComponent),
      },
      {
        path: 'reports',
        canActivate: [permissionGuard],
        data: { permissions: [PermissionCodes.ReportView] },
        loadComponent: () =>
          import('./features/questionnaires/reports-page.component').then((m) => m.ReportsPageComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'home' },
];
