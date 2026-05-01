import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IdentityLookupsApiService } from '../../services/identity-lookups-api.service';
import { ToastService } from '../../core/services/toast.service';
import { UsersApiService } from '../../services/users-api.service';
import { PagedResult } from '../../shared/models/api.types';
import { LookupItem } from '../../shared/models/lookup.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { LookupPickerComponent } from '../../shared/ui/lookup-picker.component';
import {
  AssignUserRolesRequest,
  CreateUserRequest,
  UpdateUserRequest,
  UserDto,
  UserListItemDto,
} from '../../shared/models/user.models';

type ViewMode = 'table' | 'cards';

@Component({
  selector: 'app-users-page',
  standalone: true,
  imports: [FormsModule, DatePipe, TranslatePipe, LookupPickerComponent],
  templateUrl: './users-page.component.html',
  styleUrl: './users-page.component.scss',
})
export class UsersPageComponent implements OnInit {
  private readonly api = inject(UsersApiService);
  private readonly lookupsApi = inject(IdentityLookupsApiService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  search = '';
  page = 1;
  readonly pageSize = 20;
  readonly viewMode = signal<ViewMode>('table');

  readonly result = signal<PagedResult<UserListItemDto> | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(false);

  readonly roles = signal<LookupItem[]>([]);

  readonly createOpen = signal(false);
  readonly createBusy = signal(false);
  readonly createRoleIds = signal<string[]>([]);
  readonly showCreatePassword = signal(false);
  createModel: CreateUserRequest = {
    userName: '',
    nameAr: '',
    nameEn: '',
    email: '',
    password: '',
    roleIds: [],
  };

  readonly detailsOpen = signal(false);
  readonly detailsBusy = signal(false);
  readonly detailsUser = signal<UserDto | null>(null);

  readonly editOpen = signal(false);
  readonly editBusy = signal(false);
  readonly editUserId = signal<string | null>(null);
  readonly showEditPassword = signal(false);
  editModel: UpdateUserRequest = {
    userName: '',
    nameAr: '',
    nameEn: '',
    email: '',
    newPassword: null,
  };

  readonly rolesOpen = signal(false);
  readonly roleAssignBusy = signal(false);
  readonly roleUserId = signal<string | null>(null);
  readonly assignedRoleIds = signal<string[]>([]);
  ngOnInit(): void {
    this.load();
    this.loadLookups();
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

  loadLookups(): void {
    this.lookupsApi.getRoles('', 500).subscribe({
      next: (rows) => this.roles.set(rows),
      error: () => this.toast.show(this.i18n.t('users.toast.loadRolesFailed'), 'error'),
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
    this.createModel = {
      userName: '',
      nameAr: '',
      nameEn: '',
      email: '',
      password: '',
      roleIds: [],
    };
    this.createRoleIds.set([]);
    this.showCreatePassword.set(false);
    this.createOpen.set(true);
  }

  closeCreate(): void {
    if (this.createBusy()) return;
    this.showCreatePassword.set(false);
    this.createOpen.set(false);
  }

  toggleCreatePasswordVisibility(): void {
    this.showCreatePassword.update((v) => !v);
  }

  createRoleSummary(): string {
    const ids = this.createRoleIds();
    if (ids.length === 0) return this.i18n.t('users.roles.select');
    const roleMap = new Map(this.roles().map((r) => [r.id, r.name]));
    return ids.map((id) => roleMap.get(id) ?? id).join('، ');
  }

  saveCreate(): void {
    if (!this.createModel.userName.trim() || !this.createModel.email.trim() || !this.createModel.password.trim()) {
      this.toast.show(this.i18n.t('users.toast.createRequired'), 'error');
      return;
    }
    if (this.createRoleIds().length === 0) {
      this.toast.show(this.i18n.t('users.toast.rolesRequired'), 'error');
      return;
    }

    this.createBusy.set(true);
    const body: CreateUserRequest = {
      ...this.createModel,
      userName: this.createModel.userName.trim(),
      nameAr: this.createModel.nameAr?.trim() ? this.createModel.nameAr.trim() : null,
      nameEn: this.createModel.nameEn?.trim() ? this.createModel.nameEn.trim() : null,
      email: this.createModel.email.trim(),
      password: this.createModel.password,
      roleIds: this.createRoleIds(),
    };

    this.api.create(body).subscribe({
      next: () => {
        this.createBusy.set(false);
        this.createOpen.set(false);
        this.toast.show(this.i18n.t('users.toast.created'), 'success');
        this.page = 1;
        this.load();
      },
      error: () => {
        this.createBusy.set(false);
        this.toast.show(this.i18n.t('users.toast.createFailed'), 'error');
      },
    });
  }

  openDetails(id: string): void {
    this.detailsBusy.set(true);
    this.detailsOpen.set(true);
    this.api.getById(id).subscribe({
      next: (u) => {
        this.detailsUser.set(u);
        this.detailsBusy.set(false);
      },
      error: () => {
        this.detailsBusy.set(false);
        this.detailsOpen.set(false);
        this.toast.show(this.i18n.t('users.toast.loadDetailsFailed'), 'error');
      },
    });
  }

  closeDetails(): void {
    if (this.detailsBusy()) return;
    this.detailsOpen.set(false);
    this.detailsUser.set(null);
  }

  openEdit(id: string): void {
    this.detailsOpen.set(false);
    this.editOpen.set(true);
    this.editBusy.set(true);
    this.showEditPassword.set(false);
    this.editUserId.set(id);
    this.api.getById(id).subscribe({
      next: (u) => {
        this.editModel = {
          userName: u.userName,
          nameAr: u.nameAr ?? '',
          nameEn: u.nameEn ?? '',
          email: u.email,
          newPassword: null,
        };
        this.editBusy.set(false);
      },
      error: () => {
        this.editBusy.set(false);
        this.editOpen.set(false);
        this.toast.show(this.i18n.t('users.toast.loadEditFailed'), 'error');
      },
    });
  }

  closeEdit(): void {
    if (this.editBusy()) return;
    this.editOpen.set(false);
    this.showEditPassword.set(false);
    this.editUserId.set(null);
  }

  toggleEditPasswordVisibility(): void {
    this.showEditPassword.update((v) => !v);
  }

  saveEdit(): void {
    const id = this.editUserId();
    if (!id) return;
    if (!this.editModel.userName.trim() || !this.editModel.email.trim()) {
      this.toast.show(this.i18n.t('users.toast.editRequired'), 'error');
      return;
    }

    this.editBusy.set(true);
    const body: UpdateUserRequest = {
      userName: this.editModel.userName.trim(),
      nameAr: this.editModel.nameAr?.trim() ? this.editModel.nameAr.trim() : null,
      nameEn: this.editModel.nameEn?.trim() ? this.editModel.nameEn.trim() : null,
      email: this.editModel.email.trim(),
      newPassword: this.editModel.newPassword?.trim() ? this.editModel.newPassword.trim() : null,
    };

    this.api.update(id, body).subscribe({
      next: () => {
        this.editBusy.set(false);
        this.editOpen.set(false);
        this.toast.show(this.i18n.t('users.toast.updated'), 'success');
        this.load();
      },
      error: () => {
        this.editBusy.set(false);
        this.toast.show(this.i18n.t('users.toast.updateFailed'), 'error');
      },
    });
  }

  openRoles(id: string): void {
    this.detailsOpen.set(false);
    this.rolesOpen.set(true);
    this.roleAssignBusy.set(true);
    this.roleUserId.set(id);
    this.api.getById(id).subscribe({
      next: (u) => {
        this.assignedRoleIds.set(this.roleIdsFromNames(u.roleNames));
        this.roleAssignBusy.set(false);
      },
      error: () => {
        this.roleAssignBusy.set(false);
        this.rolesOpen.set(false);
        this.toast.show(this.i18n.t('users.toast.loadUserRolesFailed'), 'error');
      },
    });
  }

  closeRoles(): void {
    if (this.roleAssignBusy()) return;
    this.rolesOpen.set(false);
    this.roleUserId.set(null);
  }

  assignRoleSummary(): string {
    const ids = this.assignedRoleIds();
    if (ids.length === 0) return this.i18n.t('users.roles.select');
    const roleMap = new Map(this.roles().map((r) => [r.id, r.name]));
    return ids.map((id) => roleMap.get(id) ?? id).join('، ');
  }

  saveRoles(): void {
    const id = this.roleUserId();
    if (!id) return;
    if (this.assignedRoleIds().length === 0) {
      this.toast.show(this.i18n.t('users.toast.rolesRequired'), 'error');
      return;
    }

    this.roleAssignBusy.set(true);
    const body: AssignUserRolesRequest = { roleIds: this.assignedRoleIds() };
    this.api.assignRoles(id, body).subscribe({
      next: (u) => {
        this.roleAssignBusy.set(false);
        this.rolesOpen.set(false);
        this.assignedRoleIds.set(this.roleIdsFromNames(u.roleNames));
        this.toast.show(this.i18n.t('users.toast.rolesUpdated'), 'success');
        this.load();
      },
      error: () => {
        this.roleAssignBusy.set(false);
        this.toast.show(this.i18n.t('users.toast.rolesUpdateFailed'), 'error');
      },
    });
  }

  activateSelected(): void {
    const id = this.detailsUser()?.id;
    if (!id) return;
    this.detailsBusy.set(true);
    this.api.patchStatus(id, true).subscribe({
      next: () => {
        this.detailsBusy.set(false);
        this.toast.show(this.i18n.t('users.toast.activated'), 'success');
        this.refreshDetails(id);
        this.load();
      },
      error: () => {
        this.detailsBusy.set(false);
        this.toast.show(this.i18n.t('users.toast.statusFailed'), 'error');
      },
    });
  }

  deactivateSelected(): void {
    const id = this.detailsUser()?.id;
    if (!id) return;
    this.detailsBusy.set(true);
    this.api.patchStatus(id, false).subscribe({
      next: () => {
        this.detailsBusy.set(false);
        this.toast.show(this.i18n.t('users.toast.deactivated'), 'success');
        this.refreshDetails(id);
        this.load();
      },
      error: () => {
        this.detailsBusy.set(false);
        this.toast.show(this.i18n.t('users.toast.statusFailed'), 'error');
      },
    });
  }

  userDisplayName(user: Pick<UserListItemDto, 'userName' | 'nameAr' | 'nameEn'>): string {
    if (this.i18n.lang() === 'ar') {
      return user.nameAr?.trim() || user.nameEn?.trim() || user.userName;
    }
    return user.nameEn?.trim() || user.nameAr?.trim() || user.userName;
  }

  private refreshDetails(id: string): void {
    this.api.getById(id).subscribe({
      next: (u) => this.detailsUser.set(u),
      error: () => this.toast.show(this.i18n.t('users.toast.loadDetailsFailed'), 'error'),
    });
  }

  private roleIdsFromNames(roleNames: readonly string[]): string[] {
    if (roleNames.length === 0) return [];
    return roleNames
      .map((n) => {
        const key = n.trim().toLowerCase();
        const r = this.roles().find((x) => x.name.trim().toLowerCase() === key);
        return r?.id ?? null;
      })
      .filter((v): v is string => v !== null);
  }
}
