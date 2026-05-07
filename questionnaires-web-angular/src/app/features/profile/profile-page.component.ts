import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { resolvedPublicAssetUrl } from '../../core/config/api-url';
import { CurrentUserProfileService } from '../../core/services/current-user-profile.service';
import { ProfileApiService } from '../../core/services/profile-api.service';
import { ToastService } from '../../core/services/toast.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import type { UserProfileDto } from '../../shared/models/profile.models';

type Daypart = 'morning' | 'afternoon' | 'evening' | 'night';

interface CalendarCell {
  day: number;
  inMonth: boolean;
  isToday: boolean;
  isWeekend: boolean;
}

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe, ReactiveFormsModule],
  templateUrl: './profile-page.component.html',
  styleUrl: './profile-page.component.scss',
})
export class ProfilePageComponent implements OnInit, OnDestroy {
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

  /** Live ticking clock used by the welcome card. */
  readonly now = signal<Date>(new Date());
  readonly quoteIndex = signal(0);
  private clockTimer: ReturnType<typeof setInterval> | null = null;
  private quoteTimer: ReturnType<typeof setInterval> | null = null;

  /** Number of motivational quote variants in i18n (`profile.welcome.quote.0..N-1`). */
  readonly quoteCount = 5;

  /** Decorative sparkles used to render a twinkling pattern. */
  readonly sparkles = Array.from({ length: 14 }, (_, i) => i);

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

  /** Daypart derived from the live clock (used to pick greeting + theme). */
  readonly daypart = computed<Daypart>(() => {
    const h = this.now().getHours();
    if (h >= 5 && h < 12) return 'morning';
    if (h >= 12 && h < 17) return 'afternoon';
    if (h >= 17 && h < 21) return 'evening';
    return 'night';
  });

  /** Localized greeting like "صباح الخير" / "Good evening". */
  readonly greeting = computed(() => this.i18n.t(`profile.welcome.${this.daypart()}`));

  /** Today's date formatted with the active locale. */
  readonly todayLabel = computed(() => {
    const locale = this.i18n.lang() === 'ar' ? 'ar-EG' : 'en-US';
    return this.now().toLocaleDateString(locale, {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  });

  /** Live HH:MM:SS string for the welcome clock — uses the device's local timezone implicitly. */
  readonly clockLabel = computed(() => {
    const locale = this.i18n.lang() === 'ar' ? 'ar-EG' : 'en-US';
    return this.now().toLocaleTimeString(locale, {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  });

  /**
   * Resolves the device's IANA timezone (e.g. "Africa/Cairo", "Asia/Dubai") and renders
   * a compact label like "GMT+3 · Cairo" so the user can see they're viewing their own
   * local time. Recomputes only on day rollover and language toggle.
   */
  readonly tzLabel = computed(() => {
    this.todayKey();
    const lang = this.i18n.lang();
    try {
      const tz = Intl.DateTimeFormat().resolvedOptions().timeZone || '';
      const cityRaw = tz.split('/').pop() ?? '';
      const city = cityRaw.replace(/_/g, ' ');

      const offsetMinutes = -this.now().getTimezoneOffset();
      const sign = offsetMinutes >= 0 ? '+' : '−';
      const abs = Math.abs(offsetMinutes);
      const hours = Math.floor(abs / 60);
      const minutes = abs % 60;
      const offsetLabel =
        minutes === 0 ? `GMT${sign}${hours}` : `GMT${sign}${hours}:${minutes.toString().padStart(2, '0')}`;

      const localizedCity = this.localizeCity(city, lang);
      return localizedCity ? `${offsetLabel} · ${localizedCity}` : offsetLabel;
    } catch {
      return '';
    }
  });

  readonly currentQuote = computed(() => this.i18n.t(`profile.welcome.quote.${this.quoteIndex()}`));

  /** Translates a small set of common IANA city names; falls back to the raw city. */
  private localizeCity(city: string, lang: 'ar' | 'en'): string {
    if (!city) return '';
    if (lang !== 'ar') return city;
    const map: Record<string, string> = {
      Cairo: 'القاهرة',
      Dubai: 'دبي',
      Riyadh: 'الرياض',
      Jeddah: 'جدة',
      Mecca: 'مكة',
      Kuwait: 'الكويت',
      Baghdad: 'بغداد',
      Doha: 'الدوحة',
      Amman: 'عمّان',
      Beirut: 'بيروت',
      Damascus: 'دمشق',
      Manama: 'المنامة',
      Muscat: 'مسقط',
      Sanaa: 'صنعاء',
      Tripoli: 'طرابلس',
      Tunis: 'تونس',
      Algiers: 'الجزائر',
      Casablanca: 'الدار البيضاء',
      Khartoum: 'الخرطوم',
      Aden: 'عدن',
      Gaza: 'غزة',
      Hebron: 'الخليل',
      Jerusalem: 'القدس',
      Istanbul: 'إسطنبول',
      London: 'لندن',
      Paris: 'باريس',
      Berlin: 'برلين',
      Moscow: 'موسكو',
      'New York': 'نيويورك',
      'Los Angeles': 'لوس أنجلوس',
      Tokyo: 'طوكيو',
    };
    return map[city] ?? city;
  }

  /** Stable day key (Y-M-D) so calendar-related computed signals only recompute at midnight. */
  private readonly todayKey = computed(() => {
    const d = this.now();
    return `${d.getFullYear()}-${d.getMonth()}-${d.getDate()}`;
  });

  /** First day of the week per locale: Sat for ar, Sun for en. */
  readonly calendarFirstDay = computed(() => (this.i18n.lang() === 'ar' ? 6 : 0));

  /** Localized "May 2026" header. */
  readonly monthLabel = computed(() => {
    this.todayKey();
    const locale = this.i18n.lang() === 'ar' ? 'ar-EG' : 'en-US';
    return new Date().toLocaleDateString(locale, { month: 'long', year: 'numeric' });
  });

  /** Short weekday names ordered by `calendarFirstDay`. */
  readonly weekdayLabels = computed(() => {
    const locale = this.i18n.lang() === 'ar' ? 'ar-EG' : 'en-US';
    const fmt = new Intl.DateTimeFormat(locale, { weekday: 'short' });
    const first = this.calendarFirstDay();
    const labels: string[] = [];
    // 1970-01-04 (UTC) is a Sunday — anchor used to derive weekday names.
    for (let i = 0; i < 7; i++) {
      const d = new Date(Date.UTC(1970, 0, 4 + ((first + i) % 7)));
      labels.push(fmt.format(d));
    }
    return labels;
  });

  /** 6×7 calendar matrix for the current month, with previous/next month padding. */
  readonly monthMatrix = computed<ReadonlyArray<ReadonlyArray<CalendarCell>>>(() => {
    this.todayKey();
    const today = new Date();
    const year = today.getFullYear();
    const month = today.getMonth();
    const dayNo = today.getDate();
    const first = this.calendarFirstDay();

    const firstOfMonth = new Date(year, month, 1);
    const offset = (firstOfMonth.getDay() - first + 7) % 7;
    const cursor = new Date(year, month, 1 - offset);

    const weeks: CalendarCell[][] = [];
    for (let w = 0; w < 6; w++) {
      const week: CalendarCell[] = [];
      for (let d = 0; d < 7; d++) {
        week.push({
          day: cursor.getDate(),
          inMonth: cursor.getMonth() === month,
          isToday:
            cursor.getFullYear() === year &&
            cursor.getMonth() === month &&
            cursor.getDate() === dayNo,
          isWeekend: cursor.getDay() === 5 || cursor.getDay() === 6,
        });
        cursor.setDate(cursor.getDate() + 1);
      }
      weeks.push(week);
    }
    return weeks;
  });

  ngOnInit(): void {
    this.loadProfile();
    this.clockTimer = setInterval(() => this.now.set(new Date()), 1000);
    this.quoteTimer = setInterval(() => {
      this.quoteIndex.update((n) => (n + 1) % this.quoteCount);
    }, 6000);
  }

  ngOnDestroy(): void {
    if (this.clockTimer) clearInterval(this.clockTimer);
    if (this.quoteTimer) clearInterval(this.quoteTimer);
  }

  session(): ReturnType<AuthService['sessionSnapshot']> {
    return this.auth.sessionSnapshot();
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
