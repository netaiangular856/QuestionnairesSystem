import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { CurrentUserProfileService } from '../../core/services/current-user-profile.service';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { LayoutStateService } from '../layout-state.service';

type IconName =
  | 'dashboard'
  | 'inbox'
  | 'poll'
  | 'approval'
  | 'template'
  | 'bar-chart'
  | 'pie-chart'
  | 'target'
  | 'spark'
  | 'sliders'
  | 'robot'
  | 'lightbulb'
  | 'kanban'
  | 'flag'
  | 'users'
  | 'employee'
  | 'building'
  | 'partners'
  | 'shield'
  | 'history'
  | 'import'
  | 'bell';

interface NavItem {
  path: string;
  labelKey: string;
  permissions: readonly string[];
  icon: IconName;
}

interface NavGroup {
  id: string;
  titleKey: string;
  items: NavItem[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
  host: {
    '[class.sidebar-host--collapsed]': 'sidebarCollapsed()',
  },
})
export class SidebarComponent {
  private readonly auth = inject(AuthService);
  private readonly i18n = inject(I18nService);
  readonly me = inject(CurrentUserProfileService);
  readonly layout = inject(LayoutStateService);
  readonly collapsedGroups = signal<Record<string, boolean>>({});
  readonly sidebarCollapsed = this.layout.sidebarCollapsed;

  /** Floating tooltip rendered outside the scrollable nav so it never gets clipped. */
  readonly tooltipText = signal<string>('');
  readonly tooltipTop = signal<number>(0);
  readonly tooltipActive = signal<boolean>(false);
  readonly tooltipIsActiveLink = signal<boolean>(false);

  private readonly groups: NavGroup[] = [
    {
      id: 'main',
      titleKey: 'nav.group.main',
      items: [
        { path: '/dashboard', labelKey: 'nav.dashboard', permissions: [], icon: 'dashboard' },
        { path: '/available-surveys', labelKey: 'nav.availableSurveys', permissions: [], icon: 'inbox' },
      ],
    },
    {
      id: 'questionnaires',
      titleKey: 'nav.group.questionnaires',
      items: [
        { path: '/surveys', labelKey: 'nav.surveys', permissions: [PermissionCodes.SurveyView], icon: 'poll' },
        {
          path: '/surveys-approval',
          labelKey: 'nav.surveysApproval',
          permissions: [PermissionCodes.SurveyApprovalView],
          icon: 'approval',
        },
        { path: '/templates', labelKey: 'nav.templates', permissions: [PermissionCodes.TemplateView], icon: 'template' },
      ],
    },
    {
      id: 'analytics',
      titleKey: 'nav.group.analytics',
      items: [
        { path: '/reports', labelKey: 'nav.reports', permissions: [PermissionCodes.ReportView], icon: 'bar-chart' },
        {
          path: '/survey-analysis',
          labelKey: 'nav.surveyAnalysis',
          permissions: [PermissionCodes.ReportView],
          icon: 'pie-chart',
        },
        {
          path: '/impact-measurement',
          labelKey: 'nav.impactMeasurement',
          permissions: [PermissionCodes.ReportView],
          icon: 'target',
        },
        {
          path: '/ai-analysis',
          labelKey: 'nav.aiAnalysis',
          permissions: [PermissionCodes.ReportView],
          icon: 'spark',
        },
        {
          path: '/ai-scope',
          labelKey: 'nav.aiScope',
          permissions: [PermissionCodes.ReportView],
          icon: 'robot',
        },
      ],
    },
    {
      id: 'programs',
      titleKey: 'nav.group.programs',
      items: [
        {
          path: '/recommendations',
          labelKey: 'nav.recommendations',
          permissions: [PermissionCodes.RecommendationView],
          icon: 'lightbulb',
        },
        { path: '/action-plans', labelKey: 'nav.actionPlans', permissions: [PermissionCodes.ActionPlanView], icon: 'kanban' },
        {
          path: '/initiatives',
          labelKey: 'nav.initiatives',
          permissions: [PermissionCodes.ActionPlanView],
          icon: 'flag',
        },
      ],
    },
    {
      id: 'organization',
      titleKey: 'nav.group.organization',
      items: [
        { path: '/users', labelKey: 'nav.users', permissions: [PermissionCodes.UserManage], icon: 'users' },
        { path: '/employees', labelKey: 'nav.employees', permissions: [PermissionCodes.EmployeeView], icon: 'employee' },
        { path: '/departments', labelKey: 'nav.departments', permissions: [PermissionCodes.DepartmentView], icon: 'building' },
        { path: '/partners', labelKey: 'nav.partners', permissions: [PermissionCodes.PartnerView], icon: 'partners' },
      ],
    },
    {
      id: 'administration',
      titleKey: 'nav.group.administration',
      items: [
        { path: '/roles', labelKey: 'nav.roles', permissions: [PermissionCodes.RoleManage], icon: 'shield' },
        { path: '/audit-logs', labelKey: 'nav.auditLogs', permissions: [PermissionCodes.AuditLogView], icon: 'history' },
        {
          path: '/data-import',
          labelKey: 'nav.dataImport',
          permissions: [PermissionCodes.DataBulkImport],
          icon: 'import',
        },
      ],
    },
    {
      id: 'ops',
      titleKey: 'nav.group.ops',
      items: [
        { path: '/notifications', labelKey: 'nav.notifications', permissions: [PermissionCodes.NotificationView], icon: 'bell' },
      ],
    },
  ];

  readonly visibleGroups = computed(() =>
    this.groups
      .map((g) => ({
        ...g,
        items: g.items.filter((i) => i.permissions.length === 0 || this.auth.hasAnyPermission(i.permissions)),
      }))
      .filter((g) => g.items.length > 0),
  );

  isGroupCollapsed(groupId: string): boolean {
    return this.collapsedGroups()[groupId] ?? false;
  }

  toggleGroup(groupId: string): void {
    if (this.sidebarCollapsed()) return;
    this.collapsedGroups.update((state) => ({ ...state, [groupId]: !state[groupId] }));
  }

  toggleSidebarCollapse(): void {
    this.layout.toggleSidebarCollapse();
    this.hideTooltip();
  }

  showTooltip(event: MouseEvent | FocusEvent, labelKey: string, isActiveLink: boolean): void {
    if (!this.sidebarCollapsed()) return;
    const target = event.currentTarget;
    if (!(target instanceof HTMLElement)) return;
    const rect = target.getBoundingClientRect();
    this.tooltipText.set(this.i18n.t(labelKey));
    this.tooltipTop.set(rect.top + rect.height / 2);
    this.tooltipIsActiveLink.set(isActiveLink);
    this.tooltipActive.set(true);
  }

  hideTooltip(): void {
    this.tooltipActive.set(false);
  }

  iconPath(icon: IconName): string {
    const map: Record<IconName, string> = {
      // 4-tile dashboard
      dashboard: 'M3 3h8v8H3V3Zm0 10h8v8H3v-8Zm10-10h8v8h-8V3Zm0 10h8v8h-8v-8Z',
      // Inbox tray with arrow (available surveys)
      inbox:
        'M19 3H5C3.9 3 3 3.9 3 5v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2Zm0 12h-4l-1.5 2h-3L9 15H5V5h14v10ZM12 6l-4 4h2.5v3h3v-3H16l-4-4Z',
      // Poll / clipboard with bars
      poll:
        'M19 3H5C3.9 3 3 3.9 3 5v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2ZM9 17H7v-7h2v7Zm4 0h-2V7h2v10Zm4 0h-2v-4h2v4Z',
      // Clipboard with checkmark (approval)
      approval:
        'M19 3h-4.18C14.4 1.84 13.3 1 12 1c-1.3 0-2.4.84-2.82 2H5C3.9 3 3 3.9 3 5v16c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2ZM12 3c.55 0 1 .45 1 1s-.45 1-1 1-1-.45-1-1 .45-1 1-1Zm-1.41 14L7 13.41l1.41-1.41 2.18 2.17L15.59 9 17 10.41 10.59 17Z',
      // Layout grid template
      template:
        'M4 4h16v4H4V4Zm0 6h7v10H4V10Zm9 0h7v4h-7v-4Zm0 6h7v4h-7v-4Z',
      // Bar chart
      'bar-chart':
        'M3 21V9h4v12H3Zm7 0V3h4v18h-4Zm7 0v-8h4v8h-4Z',
      // Pie chart
      'pie-chart':
        'M11 2v20c-5.07-.5-9-4.79-9-10S5.93 2.5 11 2Zm2.03 0v8.99H22c-.47-4.74-4.24-8.52-8.97-8.99Zm0 11.01V22c4.74-.47 8.5-4.25 8.97-8.99h-8.97Z',
      // Bullseye / target (impact)
      target:
        'M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2Zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8Zm0-13c-2.76 0-5 2.24-5 5s2.24 5 5 5 5-2.24 5-5-2.24-5-5-5Zm0 8c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3Z',
      // Sparkle / AI shimmer
      spark:
        'M19 1l-1.26 2.74L15 5l2.74 1.26L19 9l1.26-2.74L23 5l-2.74-1.26L19 1Zm-9 4L8.32 8.7 5 10l3.32 1.3L10 15l1.68-3.7L15 10l-3.32-1.3L10 5Zm9 9l-1.26 2.74L15 18l2.74 1.26L19 22l1.26-2.74L23 18l-2.74-1.26L19 14Z',
      // Sliders / controls (scope & actions)
      sliders:
        'M4 7h8v2H4V7Zm0 8h12v2H4v-2Zm12-4h4v2h-4v-2ZM14 11h2v2h-2v-2Zm2-4h4v2h-4V7Zm-4 0h2v2h-2V7Z',
      // Robot (AI assistant)
      robot:
        'M10 2h4v2h2v2h2.25A2.75 2.75 0 0 1 21 8.75v8.5A2.75 2.75 0 0 1 18.25 20H5.75A2.75 2.75 0 0 1 3 17.25v-8.5A2.75 2.75 0 0 1 5.75 6H8V4h2V2Zm8.25 6H5.75c-.41 0-.75.34-.75.75v8.5c0 .41.34.75.75.75h12.5c.41 0 .75-.34.75-.75v-8.5c0-.41-.34-.75-.75-.75ZM8.5 11.25a1.25 1.25 0 1 1 0 2.5 1.25 1.25 0 0 1 0-2.5Zm7 0a1.25 1.25 0 1 1 0 2.5 1.25 1.25 0 0 1 0-2.5ZM8 15.75h8v1.5H8v-1.5Z',
      // Lightbulb (recommendations)
      lightbulb:
        'M9 21c0 .55.45 1 1 1h4c.55 0 1-.45 1-1v-1H9v1Zm3-19C8.14 2 5 5.14 5 9c0 2.38 1.19 4.47 3 5.74V17c0 .55.45 1 1 1h6c.55 0 1-.45 1-1v-2.26c1.81-1.27 3-3.36 3-5.74 0-3.86-3.14-7-7-7Z',
      // Kanban board (action plans)
      kanban:
        'M3 3h6v18H3V3Zm8 0h6v12h-6V3Zm8 0h2v8h-2V3Z',
      // Flag (initiatives)
      flag:
        'M14.4 6 14 4H5v17h2v-7h5.6l.4 2h7V6h-5.6Z',
      // Two people (users)
      users:
        'M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5s-3 1.34-3 3 1.34 3 3 3Zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5 5 6.34 5 8s1.34 3 3 3Zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5Zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5Z',
      // Person info / employee badge card
      employee:
        'M22 4H2C.9 4 0 4.9 0 6v12c0 1.1.9 2 2 2h20c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2Zm0 14H2V6h20v12ZM9 12c1.65 0 3-1.35 3-3s-1.35-3-3-3-3 1.35-3 3 1.35 3 3 3Zm0-4c.55 0 1 .45 1 1s-.45 1-1 1-1-.45-1-1 .45-1 1-1Zm6 8H3v-1c0-2 4-3.1 6-3.1s6 1.1 6 3.1v1Zm6-3h-7v-2h7v2Zm0-4h-7V9h7v2Z',
      // Office building (departments)
      building:
        'M12 7V3H2v18h20V7H12ZM6 19H4v-2h2v2Zm0-4H4v-2h2v2Zm0-4H4V9h2v2Zm0-4H4V5h2v2Zm4 12H8v-2h2v2Zm0-4H8v-2h2v2Zm0-4H8V9h2v2Zm0-4H8V5h2v2Zm10 12h-8V9h8v10Zm-2-8h-4v2h4v-2Zm0 4h-4v2h4v-2Z',
      // Storefront (external partner organizations)
      partners:
        'M20 4H4v2h16V4Zm1 10v-2l-1-5H4l-1 5v2h1v6h10v-6h4v6h2v-6h1Zm-9 4H6v-4h6v4Z',
      // Shield with person (roles & permissions)
      shield:
        'M12 2 4 5v6.09c0 5.05 3.41 9.76 8 10.91 4.59-1.15 8-5.86 8-10.91V5l-8-3Zm0 6c1.1 0 2 .9 2 2s-.9 2-2 2-2-.9-2-2 .9-2 2-2Zm0 10c-1.85 0-5-.95-5-2.84V14.5C7 13.34 9.86 13 12 13s5 .34 5 1.5v.66c0 1.89-3.15 2.84-5 2.84Z',
      // Clock with arrow (audit history)
      history:
        'M13 3a9 9 0 0 0-9 9H1l3.89 3.89.07.14L9 12H6a7 7 0 1 1 7 7 6.96 6.96 0 0 1-4.94-2.06l-1.42 1.42A8.95 8.95 0 0 0 13 21a9 9 0 0 0 0-18Zm-1 5v5l4.28 2.54.72-1.21-3.5-2.08V8H12Z',
      // Cloud with down arrow (data import)
      import:
        'M19.35 10.04A7.49 7.49 0 0 0 12 4 7.5 7.5 0 0 0 5.35 8.04 5.99 5.99 0 0 0 0 14a6 6 0 0 0 6 6h13a5 5 0 0 0 .35-9.96ZM13 13v4h-2v-4H8l4-4 4 4h-3Z',
      // Bell (notifications)
      bell:
        'M12 22a2.5 2.5 0 0 0 2.45-2h-4.9A2.5 2.5 0 0 0 12 22Zm6.5-6V11.5a6.5 6.5 0 0 0-5.5-6.42V4a1 1 0 0 0-2 0v1.08A6.5 6.5 0 0 0 5.5 11.5V16l-2 2v1h17v-1l-2-2Z',
    };
    return map[icon];
  }
}
