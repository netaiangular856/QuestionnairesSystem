import { computed, inject, Injectable, signal } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { resolvedPublicAssetUrl } from '../config/api-url';
import type { UserProfileDto } from '../../shared/models/profile.models';
import { ProfileApiService } from './profile-api.service';

/** Keeps `/api/account/profile` in sync for shell UI (topbar avatar, sidebar). */
@Injectable({ providedIn: 'root' })
export class CurrentUserProfileService {
  private readonly auth = inject(AuthService);
  private readonly api = inject(ProfileApiService);

  private readonly profile = signal<UserProfileDto | null>(null);
  private readonly avatarVersion = signal(0);

  readonly snapshot = this.profile.asReadonly();

  readonly avatarImgSrc = computed(() => {
    const p = this.profile();
    const base = resolvedPublicAssetUrl(p?.avatarUrl ?? null);
    if (!base) return null;
    return `${base}?v=${this.avatarVersion()}`;
  });

  readonly displayLabel = computed(() => {
    const p = this.profile();
    const s = this.auth.sessionSnapshot();
    if (!s) return 'User';
    if (p) {
      const n = (p.nameAr || p.nameEn || p.userName || '').trim();
      if (n) return n;
    }
    return s.userName || s.email || 'User';
  });

  refreshFromServer(): void {
    if (!this.auth.isAuthenticated()) {
      this.clear();
      return;
    }
    this.api.getMine().subscribe({
      next: (p) => this.applyServerProfile(p),
      error: () => {
        /* leave previous snapshot */
      },
    });
  }

  applyServerProfile(p: UserProfileDto): void {
    this.profile.set(p);
    this.avatarVersion.update((v) => v + 1);
  }

  clear(): void {
    this.profile.set(null);
    this.avatarVersion.set(0);
  }
}
