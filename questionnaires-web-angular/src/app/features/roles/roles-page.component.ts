import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToastService } from '../../core/services/toast.service';
import { RolesApiService } from '../../services/roles-api.service';
import { PagedResult } from '../../shared/models/api.types';
import {
  AssignRolePermissionsRequest,
  CreateRoleRequest,
  PermissionDto,
  RoleDto,
  RoleListItemDto,
  UpdateRoleRequest,
} from '../../shared/models/role.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

type ViewMode = 'table' | 'cards';
interface PermissionGroupView {
  key: string;
  label: string;
  permissions: PermissionDto[];
}

@Component({
  selector: 'app-roles-page',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  templateUrl: './roles-page.component.html',
  styleUrl: './roles-page.component.scss',
})
export class RolesPageComponent implements OnInit {
  private readonly api = inject(RolesApiService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  search = '';
  page = 1;
  readonly pageSize = 20;
  readonly viewMode = signal<ViewMode>('table');

  readonly result = signal<PagedResult<RoleListItemDto> | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(false);

  readonly permissions = signal<PermissionDto[]>([]);

  readonly createOpen = signal(false);
  readonly createBusy = signal(false);
  createModel: CreateRoleRequest = {
    nameAr: '',
    nameEn: '',
    descriptionAr: '',
    descriptionEn: '',
    isSystemRole: false,
  };

  readonly detailsOpen = signal(false);
  readonly detailsBusy = signal(false);
  readonly detailsRole = signal<RoleDto | null>(null);
  readonly expandedDetailsGroups = signal<string[]>([]);

  readonly editOpen = signal(false);
  readonly editBusy = signal(false);
  readonly editRoleId = signal<string | null>(null);
  editModel: UpdateRoleRequest = {
    nameAr: '',
    nameEn: '',
    descriptionAr: '',
    descriptionEn: '',
  };

  readonly permissionsOpen = signal(false);
  readonly permissionsBusy = signal(false);
  readonly permissionRoleId = signal<string | null>(null);
  readonly assignedPermissionIds = signal<string[]>([]);
  readonly permissionSearch = signal('');

  readonly filteredPermissions = computed(() => this.filterPermissions(this.permissionSearch()));
  readonly groupedFilteredPermissions = computed(() => this.groupPermissions(this.filteredPermissions()));
  readonly detailsPermissionGroups = computed(() => {
    const role = this.detailsRole();
    if (!role) return [] as PermissionGroupView[];
    return this.groupPermissions(this.permissionsByCodes(role.permissionCodes));
  });

  ngOnInit(): void {
    this.load();
    this.loadPermissions();
  }

  load(): void {
    this.failed.set(false);
    this.busy.set(true);
    this.api.getPaged({ page: this.page, pageSize: this.pageSize, search: this.search || null }).subscribe({
      next: (r) => {
        this.result.set(r);
        this.busy.set(false);
      },
      error: () => {
        this.result.set(null);
        this.failed.set(true);
        this.busy.set(false);
      },
    });
  }

  loadPermissions(): void {
    this.api.getPermissions().subscribe({
      next: (rows) => this.permissions.set(rows),
      error: () => this.toast.show(this.i18n.t('roles.toast.loadPermissionsFailed'), 'error'),
    });
  }

  nextPage(): void {
    const r = this.result();
    if (!r || !r.hasNextPage) return;
    this.page += 1;
    this.load();
  }

  prevPage(): void {
    if (this.page <= 1) return;
    this.page -= 1;
    this.load();
  }

  setViewMode(mode: ViewMode): void {
    this.viewMode.set(mode);
  }

  openCreate(): void {
    this.createModel = { nameAr: '', nameEn: '', descriptionAr: '', descriptionEn: '', isSystemRole: false };
    this.createOpen.set(true);
  }

  closeCreate(): void {
    if (this.createBusy()) return;
    this.createOpen.set(false);
  }

  saveCreate(): void {
    const body: CreateRoleRequest = {
      nameAr: this.createModel.nameAr.trim(),
      nameEn: this.createModel.nameEn.trim(),
      descriptionAr: this.createModel.descriptionAr?.trim() ? this.createModel.descriptionAr.trim() : null,
      descriptionEn: this.createModel.descriptionEn?.trim() ? this.createModel.descriptionEn.trim() : null,
      isSystemRole: this.createModel.isSystemRole,
    };
    if (!body.nameAr || !body.nameEn) {
      this.toast.show(this.i18n.t('roles.toast.createRequired'), 'error');
      return;
    }

    this.createBusy.set(true);
    this.api.create(body).subscribe({
      next: () => {
        this.createBusy.set(false);
        this.createOpen.set(false);
        this.toast.show(this.i18n.t('roles.toast.created'), 'success');
        this.page = 1;
        this.load();
      },
      error: () => {
        this.createBusy.set(false);
        this.toast.show(this.i18n.t('roles.toast.createFailed'), 'error');
      },
    });
  }

  openDetails(id: string): void {
    this.detailsBusy.set(true);
    this.detailsOpen.set(true);
    this.expandedDetailsGroups.set([]);
    this.api.getById(id).subscribe({
      next: (r) => {
        this.detailsRole.set(r);
        this.detailsBusy.set(false);
      },
      error: () => {
        this.detailsBusy.set(false);
        this.detailsOpen.set(false);
        this.toast.show(this.i18n.t('roles.toast.loadDetailsFailed'), 'error');
      },
    });
  }

  closeDetails(): void {
    if (this.detailsBusy()) return;
    this.detailsOpen.set(false);
    this.detailsRole.set(null);
    this.expandedDetailsGroups.set([]);
  }

  toggleDetailsGroup(key: string): void {
    const next = new Set(this.expandedDetailsGroups());
    if (next.has(key)) next.delete(key);
    else next.add(key);
    this.expandedDetailsGroups.set(Array.from(next));
  }

  isDetailsGroupExpanded(key: string): boolean {
    return this.expandedDetailsGroups().includes(key);
  }

  openEdit(id: string): void {
    this.detailsOpen.set(false);
    this.editOpen.set(true);
    this.editBusy.set(true);
    this.editRoleId.set(id);
    this.api.getById(id).subscribe({
      next: (r) => {
        this.editModel = {
          nameAr: r.nameAr,
          nameEn: r.nameEn,
          descriptionAr: r.descriptionAr ?? '',
          descriptionEn: r.descriptionEn ?? '',
        };
        this.editBusy.set(false);
      },
      error: () => {
        this.editBusy.set(false);
        this.editOpen.set(false);
        this.toast.show(this.i18n.t('roles.toast.loadEditFailed'), 'error');
      },
    });
  }

  closeEdit(): void {
    if (this.editBusy()) return;
    this.editOpen.set(false);
    this.editRoleId.set(null);
  }

  saveEdit(): void {
    const id = this.editRoleId();
    if (!id) return;
    const body: UpdateRoleRequest = {
      nameAr: this.editModel.nameAr.trim(),
      nameEn: this.editModel.nameEn.trim(),
      descriptionAr: this.editModel.descriptionAr?.trim() ? this.editModel.descriptionAr.trim() : null,
      descriptionEn: this.editModel.descriptionEn?.trim() ? this.editModel.descriptionEn.trim() : null,
    };
    if (!body.nameAr || !body.nameEn) {
      this.toast.show(this.i18n.t('roles.toast.editRequired'), 'error');
      return;
    }

    this.editBusy.set(true);
    this.api.update(id, body).subscribe({
      next: () => {
        this.editBusy.set(false);
        this.editOpen.set(false);
        this.toast.show(this.i18n.t('roles.toast.updated'), 'success');
        this.load();
      },
      error: () => {
        this.editBusy.set(false);
        this.toast.show(this.i18n.t('roles.toast.updateFailed'), 'error');
      },
    });
  }

  openPermissions(id: string): void {
    this.detailsOpen.set(false);
    this.permissionsOpen.set(true);
    this.permissionsBusy.set(true);
    this.permissionRoleId.set(id);
    this.permissionSearch.set('');

    this.api.getById(id).subscribe({
      next: (r) => {
        this.assignedPermissionIds.set(this.permissionIdsFromCodes(r.permissionCodes));
        this.permissionsBusy.set(false);
      },
      error: () => {
        this.permissionsBusy.set(false);
        this.permissionsOpen.set(false);
        this.toast.show(this.i18n.t('roles.toast.loadRolePermissionsFailed'), 'error');
      },
    });
  }

  closePermissions(): void {
    if (this.permissionsBusy()) return;
    this.permissionsOpen.set(false);
    this.permissionRoleId.set(null);
  }

  toggleAssignedPermission(id: string): void {
    const next = new Set(this.assignedPermissionIds());
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.assignedPermissionIds.set(Array.from(next));
  }

  permissionSelected(id: string): boolean {
    return this.assignedPermissionIds().includes(id);
  }

  savePermissions(): void {
    const id = this.permissionRoleId();
    if (!id) return;

    this.permissionsBusy.set(true);
    const body: AssignRolePermissionsRequest = { permissionIds: this.assignedPermissionIds() };
    this.api.assignPermissions(id, body).subscribe({
      next: () => {
        this.permissionsBusy.set(false);
        this.permissionsOpen.set(false);
        this.toast.show(this.i18n.t('roles.toast.permissionsUpdated'), 'success');
        this.load();
      },
      error: () => {
        this.permissionsBusy.set(false);
        this.toast.show(this.i18n.t('roles.toast.permissionsUpdateFailed'), 'error');
      },
    });
  }

  private filterPermissions(search: string): PermissionDto[] {
    const term = search.trim().toLowerCase();
    if (!term) return this.permissions();
    return this.permissions().filter(
      (x) =>
        x.nameAr.toLowerCase().includes(term) ||
        x.nameEn.toLowerCase().includes(term) ||
        x.code.toLowerCase().includes(term) ||
        x.module.toLowerCase().includes(term),
    );
  }

  private permissionIdsFromCodes(codes: readonly string[]): string[] {
    if (codes.length === 0) return [];
    const byCode = new Map(this.permissions().map((p) => [p.code.toLowerCase(), p.id]));
    return codes.map((c) => byCode.get(c.toLowerCase()) ?? null).filter((v): v is string => v !== null);
  }

  private permissionsByCodes(codes: readonly string[]): PermissionDto[] {
    if (codes.length === 0) return [];
    const byCode = new Map(this.permissions().map((p) => [p.code.toLowerCase(), p]));
    return codes
      .map((code) => byCode.get(code.toLowerCase()) ?? null)
      .filter((permission): permission is PermissionDto => permission !== null);
  }

  private groupPermissions(rows: readonly PermissionDto[]): PermissionGroupView[] {
    const groups = new Map<string, PermissionDto[]>();
    for (const permission of rows) {
      const key = this.permissionGroupKey(permission);
      const list = groups.get(key) ?? [];
      list.push(permission);
      groups.set(key, list);
    }

    const order = new Map<string, number>([
      ['roles.groups.questionnaires', 10],
      ['roles.groups.analytics', 20],
      ['roles.groups.programs', 30],
      ['roles.groups.organization', 40],
      ['roles.groups.administration', 50],
      ['roles.groups.ops', 60],
      ['roles.groups.other', 999],
    ]);

    return Array.from(groups.entries())
      .sort((a, b) => (order.get(a[0]) ?? 500) - (order.get(b[0]) ?? 500) || a[0].localeCompare(b[0]))
      .map(([key, perms]) => ({
        key,
        label: this.i18n.t(key),
        permissions: perms.sort((a, b) =>
          this.permissionDisplayName(a).localeCompare(this.permissionDisplayName(b), this.i18n.lang()),
        ),
      }));
  }

  roleDisplayName(role: Pick<RoleListItemDto, 'nameAr' | 'nameEn'>): string {
    return this.i18n.lang() === 'ar' ? role.nameAr || role.nameEn : role.nameEn || role.nameAr;
  }

  roleDisplayDescription(role: Pick<RoleListItemDto, 'descriptionAr' | 'descriptionEn'>): string | null {
    return this.i18n.lang() === 'ar'
      ? role.descriptionAr ?? role.descriptionEn ?? null
      : role.descriptionEn ?? role.descriptionAr ?? null;
  }

  permissionDisplayName(permission: Pick<PermissionDto, 'nameAr' | 'nameEn'>): string {
    return this.i18n.lang() === 'ar'
      ? permission.nameAr || permission.nameEn
      : permission.nameEn || permission.nameAr;
  }

  private permissionGroupKey(permission: Pick<PermissionDto, 'code' | 'module'>): string {
    const code = permission.code.trim().toUpperCase();

    // Core product areas — mirrors sidebar grouping for a more predictable UX.
    if (
      code.startsWith('SURVEY_') ||
      code.startsWith('QUESTION_') ||
      code.startsWith('TEMPLATE_') ||
      code.startsWith('PARTICIPANT_') ||
      code.startsWith('RESPONSE_')
    ) {
      return 'roles.groups.questionnaires';
    }

    if (code.startsWith('REPORT_')) {
      return 'roles.groups.analytics';
    }

    if (code.startsWith('RECOMMENDATION_') || code.startsWith('ACTION_PLAN_') || code.startsWith('INITIATIVE_')) {
      return 'roles.groups.programs';
    }

    if (
      code.startsWith('DEPARTMENT_') ||
      code.startsWith('EMPLOYEE_') ||
      code.startsWith('PARTNER_')
    ) {
      return 'roles.groups.organization';
    }

    if (
      code.startsWith('USER_') ||
      code.startsWith('ROLE_') ||
      code.startsWith('AUDIT_LOG_') ||
      code.startsWith('NOTIFICATION_')
    ) {
      return 'roles.groups.administration';
    }

    if (code.startsWith('DATA_') || code.startsWith('INTEGRATION_') || code.startsWith('LOOKUP_') || code.startsWith('SETTINGS_')) {
      return 'roles.groups.ops';
    }

    // Fallback: backend module (older permissions).
    const value = permission.module.trim().toLowerCase();
    if (value.includes('audit')) return 'roles.groups.administration';
    if (value.includes('notification')) return 'roles.groups.administration';
    if (value.includes('analytics') || value.includes('intelligence') || value.includes('insight')) {
      return 'roles.groups.analytics';
    }
    if (value.includes('employee') || value.includes('department') || value.includes('partner')) {
      return 'roles.groups.organization';
    }

    return 'roles.groups.other';
  }
}
