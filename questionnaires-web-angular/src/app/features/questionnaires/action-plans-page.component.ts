import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ActionPlansApiService } from '../../services/action-plans-api.service';
import { ActionPlanDto, ActionPlanStatus } from '../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { qLocalizedTitle, qPlanStatusKey } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

const VIEW_MODE_KEY = 'qplans.viewMode';

@Component({
  selector: 'app-action-plans-page',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './action-plans-page.component.html',
  styleUrl: './action-plans-page.component.scss',
})
export class ActionPlansPageComponent implements OnInit {
  private readonly api = inject(ActionPlansApiService);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  readonly canManage = () => this.auth.hasPermission(PermissionCodes.ActionPlanManage);

  readonly items = signal<ActionPlanDto[]>([]);
  readonly failed = signal(false);
  readonly busy = signal(true);

  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly totalCount = signal(0);

  readonly totalPages = computed(() => {
    const n = this.totalCount();
    const ps = this.pageSize();
    return ps <= 0 ? 1 : Math.max(1, Math.ceil(n / ps));
  });

  readonly viewMode = signal<'cards' | 'table'>(this.readViewMode());

  ngOnInit(): void {
    this.loadPage();
  }

  private readViewMode(): 'cards' | 'table' {
    try {
      return globalThis.localStorage?.getItem(VIEW_MODE_KEY) === 'table' ? 'table' : 'cards';
    } catch {
      return 'cards';
    }
  }

  setViewMode(mode: 'cards' | 'table'): void {
    this.viewMode.set(mode);
    try {
      globalThis.localStorage?.setItem(VIEW_MODE_KEY, mode);
    } catch {
      /* ignore */
    }
  }

  loadPage(): void {
    this.busy.set(true);
    this.failed.set(false);
    this.api.listPaged(this.page(), this.pageSize()).subscribe({
      next: (paged) => {
        this.items.set([...paged.items]);
        this.totalCount.set(paged.totalCount);
        this.busy.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.busy.set(false);
      },
    });
  }

  goPrevPage(): void {
    if (this.page() <= 1) return;
    this.page.update((p) => p - 1);
    this.loadPage();
  }

  goNextPage(): void {
    if (this.page() >= this.totalPages()) return;
    this.page.update((p) => p + 1);
    this.loadPage();
  }

  setPageSize(n: number): void {
    const size = Math.min(100, Math.max(6, n));
    this.pageSize.set(size);
    this.page.set(1);
    this.loadPage();
  }

  dateLocale(): string {
    return this.i18n.lang() === 'ar' ? 'ar-SA' : 'en-US';
  }

  ownerInitial(p: ActionPlanDto): string {
    const name = p.ownerDisplayName?.trim();
    if (!name) return '';
    const first = name.charAt(0);
    return first.toLocaleUpperCase(this.i18n.lang() === 'ar' ? 'ar' : 'en');
  }

  hasPeriod(p: ActionPlanDto): boolean {
    return !!(p.startDateUtc && p.endDateUtc);
  }

  /** Elapsed percent between start/end dates, clamped 0..100. null if no period. */
  planProgress(p: ActionPlanDto): number | null {
    if (!p.startDateUtc || !p.endDateUtc) return null;
    const start = new Date(p.startDateUtc).getTime();
    const end = new Date(p.endDateUtc).getTime();
    const now = Date.now();
    if (!isFinite(start) || !isFinite(end) || end <= start) return null;
    const pct = ((now - start) / (end - start)) * 100;
    return Math.max(0, Math.min(100, Math.round(pct)));
  }

  titleOf(p: ActionPlanDto): string {
    return qLocalizedTitle(this.i18n.lang(), p.titleAr, p.titleEn);
  }

  statusKey(p: ActionPlanDto): string {
    return qPlanStatusKey(p.status);
  }

  /** Maps plan status to recommendation card accent */
  planCardClass(p: ActionPlanDto): string {
    switch (p.status) {
      case ActionPlanStatus.Draft:
        return 'rec-card--draft';
      case ActionPlanStatus.Active:
        return 'rec-card--active';
      case ActionPlanStatus.Completed:
        return 'rec-card--implemented';
      case ActionPlanStatus.Cancelled:
        return 'rec-card--dismissed';
      default:
        return 'rec-card--draft';
    }
  }

  /** Maps plan status to `.rec-status--*` suffix (same palette as recommendations) */
  planStatusVariant(p: ActionPlanDto): string {
    switch (p.status) {
      case ActionPlanStatus.Draft:
        return 'draft';
      case ActionPlanStatus.Active:
        return 'active';
      case ActionPlanStatus.Completed:
        return 'implemented';
      case ActionPlanStatus.Cancelled:
        return 'dismissed';
      default:
        return 'draft';
    }
  }
}
