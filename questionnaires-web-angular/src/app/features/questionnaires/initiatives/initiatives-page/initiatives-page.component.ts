import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InitiativesApiService } from '../../../../services/initiatives-api.service';
import { InitiativeListItemDto, InitiativeStatus } from '../../../../shared/models/questionnaire.models';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';
import { qInitiativeStatusKey, qLocalizedTitle } from '../../../../shared/questionnaires/q-display';

const VIEW_MODE_KEY = 'qinit.viewMode';

@Component({
  selector: 'app-initiatives-page',
  standalone: true,
  imports: [DatePipe, FormsModule, RouterLink, TranslatePipe],
  templateUrl: './initiatives-page.component.html',
  styleUrl: './initiatives-page.component.scss',
})
export class InitiativesPageComponent implements OnInit {
  private readonly api = inject(InitiativesApiService);
  readonly i18n = inject(I18nService);

  readonly items = signal<InitiativeListItemDto[]>([]);
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

  titleOf(r: InitiativeListItemDto): string {
    return qLocalizedTitle(this.i18n.lang(), r.titleAr, r.titleEn);
  }

  planTitleOf(r: InitiativeListItemDto): string {
    return qLocalizedTitle(this.i18n.lang(), r.actionPlanTitleAr, r.actionPlanTitleEn);
  }

  statusKey(r: InitiativeListItemDto): string {
    return qInitiativeStatusKey(r.status);
  }

  initiativeCardClass(r: InitiativeListItemDto): string {
    switch (r.status) {
      case InitiativeStatus.Planned:
        return 'rec-card--draft';
      case InitiativeStatus.InProgress:
        return 'rec-card--active';
      case InitiativeStatus.Completed:
        return 'rec-card--implemented';
      case InitiativeStatus.AtRisk:
        return 'rec-card--atrisk';
      case InitiativeStatus.Cancelled:
        return 'rec-card--dismissed';
      default:
        return 'rec-card--draft';
    }
  }

  /** Maps initiative status to recommendation-style status chip suffix */
  statusVariant(r: InitiativeListItemDto): string {
    switch (r.status) {
      case InitiativeStatus.Planned:
        return 'draft';
      case InitiativeStatus.InProgress:
        return 'active';
      case InitiativeStatus.Completed:
        return 'implemented';
      case InitiativeStatus.AtRisk:
        return 'risk';
      case InitiativeStatus.Cancelled:
        return 'dismissed';
      default:
        return 'draft';
    }
  }

  dateLocale(): string {
    return this.i18n.lang() === 'ar' ? 'ar-SA' : 'en-US';
  }

  ownerInitial(r: InitiativeListItemDto): string {
    const name = r.ownerDisplayName?.trim();
    if (!name) return '';
    const first = name.charAt(0);
    return first.toLocaleUpperCase(this.i18n.lang() === 'ar' ? 'ar' : 'en');
  }

  /** Days remaining to target date. Negative if past due. null if none. */
  daysToTarget(r: InitiativeListItemDto): number | null {
    if (!r.targetDateUtc) return null;
    const target = new Date(r.targetDateUtc).getTime();
    if (!isFinite(target)) return null;
    const days = (target - Date.now()) / (1000 * 60 * 60 * 24);
    return Math.round(days);
  }

  targetToneClass(r: InitiativeListItemDto): string {
    const d = this.daysToTarget(r);
    if (d === null) return 'apl-target--none';
    if (d < 0) return 'apl-target--overdue';
    if (d <= 7) return 'apl-target--soon';
    return 'apl-target--ok';
  }

  targetText(r: InitiativeListItemDto): string {
    const d = this.daysToTarget(r);
    if (d === null) return this.i18n.t('q.init.card.noTarget');
    if (d < 0) return this.i18n.t('q.init.card.overdue').replace('{n}', String(Math.abs(d)));
    if (d === 0) return this.i18n.t('q.init.card.dueToday');
    if (d === 1) return this.i18n.t('q.init.card.dueTomorrow');
    return this.i18n.t('q.init.card.daysLeft').replace('{n}', String(d));
  }
}
