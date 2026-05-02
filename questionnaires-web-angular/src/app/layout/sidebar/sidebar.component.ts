import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { CurrentUserProfileService } from '../../core/services/current-user-profile.service';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { LayoutStateService } from '../layout-state.service';

interface NavItem {
  path: string;
  labelKey: string;
  permissions: readonly string[];
  icon: 'home' | 'users' | 'roles' | 'audit' | 'bell' | 'survey' | 'template' | 'recommend' | 'plan' | 'report';
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
  readonly me = inject(CurrentUserProfileService);
  readonly layout = inject(LayoutStateService);
  readonly collapsedGroups = signal<Record<string, boolean>>({});
  readonly sidebarCollapsed = signal(false);

  private readonly groups: NavGroup[] = [
    {
      id: 'main',
      titleKey: 'nav.group.main',
      items: [
        { path: '/dashboard', labelKey: 'nav.dashboard', permissions: [], icon: 'home' },
        { path: '/available-surveys', labelKey: 'nav.availableSurveys', permissions: [], icon: 'survey' },
      ],
    },
    {
      id: 'questionnaires',
      titleKey: 'nav.group.questionnaires',
      items: [
        { path: '/surveys', labelKey: 'nav.surveys', permissions: [PermissionCodes.SurveyView], icon: 'survey' },
        { path: '/surveys-approval', labelKey: 'nav.surveysApproval', permissions: [PermissionCodes.SurveyApprovalView], icon: 'audit' },
        { path: '/templates', labelKey: 'nav.templates', permissions: [PermissionCodes.TemplateView], icon: 'template' },
        {
          path: '/recommendations',
          labelKey: 'nav.recommendations',
          permissions: [PermissionCodes.RecommendationView],
          icon: 'recommend',
        },
        { path: '/action-plans', labelKey: 'nav.actionPlans', permissions: [PermissionCodes.ActionPlanView], icon: 'plan' },
        { path: '/reports', labelKey: 'nav.reports', permissions: [PermissionCodes.ReportView], icon: 'report' },
      ],
    },
    {
      id: 'identity',
      titleKey: 'nav.group.identity',
      items: [
        { path: '/users', labelKey: 'nav.users', permissions: [PermissionCodes.UserManage], icon: 'users' },
        { path: '/employees', labelKey: 'nav.employees', permissions: [PermissionCodes.EmployeeView], icon: 'users' },
        { path: '/departments', labelKey: 'nav.departments', permissions: [PermissionCodes.DepartmentView], icon: 'template' },
        { path: '/partners', labelKey: 'nav.partners', permissions: [PermissionCodes.PartnerView], icon: 'audit' },
        { path: '/roles', labelKey: 'nav.roles', permissions: [PermissionCodes.RoleManage], icon: 'roles' },
        { path: '/audit-logs', labelKey: 'nav.auditLogs', permissions: [PermissionCodes.AuditLogView], icon: 'audit' },
      ],
    },
    {
      id: 'ops',
      titleKey: 'nav.group.ops',
      items: [{ path: '/notifications', labelKey: 'nav.notifications', permissions: [PermissionCodes.NotificationView], icon: 'bell' }],
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
    this.sidebarCollapsed.update((v) => !v);
  }

  iconPath(icon: NavItem['icon']): string {
    const map: Record<NavItem['icon'], string> = {
      home: 'M12 3.5 3.5 10v10h5.5v-6h6v6h5.5V10L12 3.5Z',
      users:
        'M16 11a4 4 0 1 0-2.83-6.83A4 4 0 0 0 16 11Zm-8 1a3.5 3.5 0 1 0-2.47-5.97A3.5 3.5 0 0 0 8 12Zm0 2c-2.83 0-6 1.38-6 3v1h9v-1c0-1.62-3.17-3-3-3Zm8-1c-1.02 0-2.08.2-3 .56 1.2.74 2 1.76 2 2.94v1h7v-1c0-1.74-3.13-3.5-6-3.5Z',
      roles:
        'M12 2.5 4.5 5.7v5.1c0 5 3.2 9.6 7.5 10.9 4.3-1.3 7.5-5.9 7.5-10.9V5.7L12 2.5Zm-1 12.8-3-3 1.4-1.4 1.6 1.6 3.6-3.6 1.4 1.4-5 5Z',
      audit: 'M6 3h9l4 4v14H6V3Zm2 2v14h9V8h-3V5H8Zm1 6h7v2H9v-2Zm0 4h7v2H9v-2Z',
      bell: 'M12 3a5.5 5.5 0 0 0-5.5 5.5v2.8L4.7 14.4a1 1 0 0 0 .87 1.5h12.86a1 1 0 0 0 .87-1.5L17.5 11.3V8.5A5.5 5.5 0 0 0 12 3Zm0 18a2.5 2.5 0 0 0 2.45-2h-4.9A2.5 2.5 0 0 0 12 21Z',
      survey:
        'M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6Zm-1 2.5L18.5 9H13V4.5ZM8 12h8v2H8v-2Zm0 4h8v2H8v-2Z',
      template:
        'M4 6h7V4H4c-1.1 0-2 .9-2 2v11h2V6Zm16 4h-8v10h8c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2Zm-2 8h-4v-2h4v2Zm0-4h-4v-2h4v2ZM10 8H8v12c0 1.1.9 2 2 2h8v-2h-8V8Z',
      recommend:
        'm12 3 1.8 3.6 4 .6-2.9 2.8.7 4L12 14.9 8.4 14.1l.7-4L6.2 7.2l4-.6L12 3Zm-7 14v2h14v-2H5Zm2 4h10v-2H7v2Z',
      plan: 'M5 3h14v2H5V3Zm0 6h8v2H5V9Zm0 6h5v2H5v-2Zm12-4h-5v8h5v-8Zm-2 2v4h-1v-4h1Z',
      report: 'M4 19V5h2v14H4Zm4 0V9h2v10H8Zm4 0v-6h2v6h-4Zm4 0V7h2v12h-2Z',
    };
    return map[icon];
  }
}
