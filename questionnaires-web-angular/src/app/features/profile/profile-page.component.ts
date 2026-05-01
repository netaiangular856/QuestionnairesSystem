import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService, type AuthSession } from '../../core/auth/auth.service';
import { resolvedPublicAssetUrl } from '../../core/config/api-url';
import { CurrentUserProfileService } from '../../core/services/current-user-profile.service';
import { ProfileApiService } from '../../core/services/profile-api.service';
import { ToastService } from '../../core/services/toast.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import type { UserProfileDto } from '../../shared/models/profile.models';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe, ReactiveFormsModule],
  templateUrl: './profile-page.component.html',
  styleUrl: './profile-page.component.scss',
})
export class ProfilePageComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly profileApi = inject(ProfileApiService);
  private readonly shellProfile = inject(CurrentUserProfileService);
  private readonly fb = inject(FormBuilder);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  readonly profile = signal<UserProfileDto | null>(null);
  readonly busy = signal(false);
  readonly avatarVersion = signal(0);
  readonly editModalOpen = signal(false);

  readonly profileForm = this.fb.nonNullable.group({
    nameAr: [''],
    nameEn: [''],
    email: ['', [Validators.required, Validators.email]],
  });

  readonly headline = computed(() => {
    const p = this.profile();
    if (p) {
      return (p.nameAr || p.nameEn || p.userName || '—').trim() || '—';
    }
    const s = this.auth.sessionSnapshot();
    return s?.userName || '—';
  });

  readonly avatarSrc = computed(() => {
    const p = this.profile();
    const base = resolvedPublicAssetUrl(p?.avatarUrl ?? null);
    if (!base) return null;
    return `${base}?v=${this.avatarVersion()}`;
  });

  readonly hasAvatar = computed(() => !!this.profile()?.avatarUrl?.trim());

  ngOnInit(): void {
    this.loadProfile();
  }

  session(): ReturnType<AuthService['sessionSnapshot']> {
    return this.auth.sessionSnapshot();
  }

  sortedPermissions(s: AuthSession): string[] {
    return [...s.permissions].sort((a, b) => a.localeCompare(b, undefined, { sensitivity: 'base' }));
  }

  /** Label for chips: uses `permission.CODE` from i18n when the JWT tail matches a known code. */
  permissionLabel(code: string): string {
    const norm = this.normalizePermissionCode(code);
    if (!norm) return code;
    const key = `permission.${norm}`;
    const translated = this.i18n.t(key);
    if (translated !== key) return translated;
    return this.permissionDisplayLabel(code);
  }

  /** Pretty English-style fallback when no translation key exists. */
  permissionDisplayLabel(code: string): string {
    const c = code.trim();
    if (!c) return code;
    const dot = c.lastIndexOf('.');
    const tail = (dot === -1 ? c : c.slice(dot + 1)).trim();
    const core = tail || c;
    const parts = core.split('_').filter((p) => p.length > 0);
    if (parts.length === 0) return core;
    return parts.map((p) => p.charAt(0).toUpperCase() + p.slice(1).toLowerCase()).join(' ');
  }

  private normalizePermissionCode(code: string): string {
    const c = code.trim();
    if (!c) return '';
    const dot = c.lastIndexOf('.');
    const tail = (dot === -1 ? c : c.slice(dot + 1)).trim();
    return tail.toUpperCase().replace(/\s+/g, '_');
  }

  openEditModal(): void {
    const p = this.profile();
    if (p) {
      this.profileForm.patchValue({
        nameAr: p.nameAr ?? '',
        nameEn: p.nameEn ?? '',
        email: p.email,
      });
    }
    this.editModalOpen.set(true);
  }

  closeEditModal(): void {
    this.editModalOpen.set(false);
  }

  loadProfile(): void {
    this.busy.set(true);
    this.profileApi
      .getMine()
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: (p) => {
          this.profile.set(p);
          this.profileForm.patchValue({
            nameAr: p.nameAr ?? '',
            nameEn: p.nameEn ?? '',
            email: p.email,
          });
        },
        error: () => {
          this.toast.show(this.i18n.t('profile.loadFailed'), 'error');
        },
      });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }
    const v = this.profileForm.getRawValue();
    this.busy.set(true);
    this.profileApi
      .updateMine({
        nameAr: v.nameAr.trim() ? v.nameAr : null,
        nameEn: v.nameEn.trim() ? v.nameEn : null,
        email: v.email.trim(),
      })
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: (p) => {
          this.profile.set(p);
          this.shellProfile.applyServerProfile(p);
          this.avatarVersion.update((n) => n + 1);
          this.editModalOpen.set(false);
          this.toast.show(this.i18n.t('profile.detailsSaved'), 'success');
        },
        error: () => {
          this.toast.show(this.i18n.t('profile.detailsSaveFailed'), 'error');
        },
      });
  }

  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.busy.set(true);
    this.profileApi
      .uploadAvatar(file)
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: (p) => {
          this.profile.set(p);
          this.shellProfile.applyServerProfile(p);
          this.avatarVersion.update((n) => n + 1);
          this.toast.show(this.i18n.t('profile.photoUploaded'), 'success');
        },
        error: () => {
          this.toast.show(this.i18n.t('profile.photoUploadFailed'), 'error');
        },
      });
  }

  removeAvatar(): void {
    this.busy.set(true);
    this.profileApi
      .clearAvatar()
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: (p) => {
          this.profile.set(p);
          this.shellProfile.applyServerProfile(p);
          this.avatarVersion.update((n) => n + 1);
          this.toast.show(this.i18n.t('profile.photoRemoved'), 'success');
        },
        error: () => {
          this.toast.show(this.i18n.t('profile.photoUploadFailed'), 'error');
        },
      });
  }

  avatarFallbackLetter(): string {
    const p = this.profile();
    const s = this.session();
    const raw = (p?.userName || p?.email || s?.userName || s?.email || '?').trim();
    return raw.charAt(0).toUpperCase();
  }
}
