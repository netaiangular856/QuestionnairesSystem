import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subscription, forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { QuestionnaireLookupsApiService } from '../../services/questionnaire-lookups-api.service';
import { RecommendationsApiService } from '../../services/recommendations-api.service';
import { LookupItemDto, RecommendationDto, RecommendationStatus } from '../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { qLocalizedTitle, qRecStatusKey } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { RecommendationCreatePanelComponent } from './recommendation-create-panel.component';
import { RecommendationEditPanelComponent } from './recommendation-edit-panel.component';

const VIEW_MODE_KEY = 'qrec.viewMode';

@Component({
  selector: 'app-recommendations-page',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslatePipe, RecommendationCreatePanelComponent, RecommendationEditPanelComponent],
  templateUrl: './recommendations-page.component.html',
  styleUrl: './recommendations-page.component.scss',
})
export class RecommendationsPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(RecommendationsApiService);
  private readonly lookupsApi = inject(QuestionnaireLookupsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  readonly canRecommendManage = this.auth.hasPermission(PermissionCodes.RecommendationManage);

  readonly items = signal<RecommendationDto[]>([]);
  readonly surveyLookup = signal<LookupItemDto[]>([]);
  readonly failed = signal(false);
  readonly busy = signal(true);
  readonly prefillSurveyId = signal<string | null>(null);
  readonly createModalOpen = signal(false);

  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly totalCount = signal(0);

  readonly totalPages = computed(() => {
    const n = this.totalCount();
    const ps = this.pageSize();
    return ps <= 0 ? 1 : Math.max(1, Math.ceil(n / ps));
  });

  readonly viewMode = signal<'cards' | 'table'>(this.readViewMode());

  readonly editModalOpen = signal(false);
  readonly editingId = signal<string | null>(null);

  readonly deleteModalOpen = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly deleteBusy = signal(false);

  readonly detailModalOpen = signal(false);
  readonly detailLoading = signal(false);
  readonly detailFailed = signal(false);
  readonly detailItem = signal<RecommendationDto | null>(null);

  private querySub?: Subscription;

  ngOnInit(): void {
    this.querySub = this.route.queryParamMap.subscribe((q) => {
      const raw = q.get('surveyId');
      this.prefillSurveyId.set(raw && raw.length >= 32 ? raw : null);
    });

    this.loadPage();
  }

  ngOnDestroy(): void {
    this.querySub?.unsubscribe();
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
    forkJoin({
      paged: this.api.listPaged(this.page(), this.pageSize()),
      surveys: this.lookupsApi.getSurveys('', 500).pipe(catchError(() => of([] as LookupItemDto[]))),
    }).subscribe({
      next: ({ paged, surveys }) => {
        this.items.set([...paged.items]);
        this.totalCount.set(paged.totalCount);
        this.surveyLookup.set([...surveys]);
        this.busy.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.busy.set(false);
      },
    });
  }

  refreshAfterMutation(): void {
    this.loadPage();
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

  openCreateModal(): void {
    this.createModalOpen.set(true);
  }

  closeCreateModal(): void {
    this.createModalOpen.set(false);
  }

  onCreateModalSuccess(): void {
    this.refreshAfterMutation();
    this.closeCreateModal();
    if (this.page() > 1 && this.items().length === 0) this.page.set(1);
  }

  openEditModal(id: string): void {
    this.editingId.set(id);
    this.editModalOpen.set(true);
  }

  closeEditModal(): void {
    this.editModalOpen.set(false);
    this.editingId.set(null);
  }

  onEditSaved(): void {
    this.closeEditModal();
    this.refreshAfterMutation();
  }

  openDeleteModal(id: string): void {
    this.deletingId.set(id);
    this.deleteModalOpen.set(true);
  }

  closeDeleteModal(): void {
    this.deleteModalOpen.set(false);
    this.deletingId.set(null);
  }

  openDetailModal(id: string): void {
    this.detailModalOpen.set(true);
    this.detailLoading.set(true);
    this.detailFailed.set(false);
    this.detailItem.set(null);
    this.api.getById(id).subscribe({
      next: (row) => {
        this.detailItem.set(row);
        this.detailLoading.set(false);
      },
      error: () => {
        this.detailFailed.set(true);
        this.detailLoading.set(false);
      },
    });
  }

  closeDetailModal(): void {
    this.detailModalOpen.set(false);
    this.detailItem.set(null);
    this.detailFailed.set(false);
  }

  /** Close detail and open edit for same recommendation */
  detailThenEdit(): void {
    const row = this.detailItem();
    if (!row) return;
    this.closeDetailModal();
    this.openEditModal(row.id);
  }

  confirmDelete(): void {
    const id = this.deletingId();
    if (!id) return;
    this.deleteBusy.set(true);
    this.api.delete(id).subscribe({
      next: () => {
        this.deleteBusy.set(false);
        this.closeDeleteModal();
        this.toast.show(this.i18n.t('q.rec.delete.success'), 'success');
        if (this.items().length <= 1 && this.page() > 1) {
          this.page.update((p) => Math.max(1, p - 1));
        }
        this.refreshAfterMutation();
      },
      error: () => {
        this.deleteBusy.set(false);
        this.toast.show(this.i18n.t('q.rec.delete.error'), 'error');
      },
    });
  }

  surveyTitleFor(surveyId: string | null): string {
    if (!surveyId) return '—';
    const row = this.surveyLookup().find((x) => x.id === surveyId);
    const n = row?.name?.trim();
    return n ? n : '—';
  }

  priorityTierKey(r: RecommendationDto): string {
    const p = r.priority;
    if (p >= 7) return 'q.rec.priorityTier.high';
    if (p >= 4) return 'q.rec.priorityTier.mid';
    return 'q.rec.priorityTier.low';
  }

  titleOf(r: RecommendationDto): string {
    return qLocalizedTitle(this.i18n.lang(), r.titleAr, r.titleEn);
  }

  descOf(r: RecommendationDto): string | null {
    const ar = r.descriptionAr?.trim() ?? '';
    const en = r.descriptionEn?.trim() ?? '';
    const raw = qLocalizedTitle(this.i18n.lang(), ar, en);
    if (!raw) return null;
    return raw.length > 160 ? `${raw.slice(0, 157)}…` : raw;
  }

  statusKey(r: RecommendationDto): string {
    return qRecStatusKey(r.status);
  }

  statusVariant(r: RecommendationDto): 'draft' | 'active' | 'implemented' | 'dismissed' {
    switch (r.status) {
      case RecommendationStatus.Draft:
        return 'draft';
      case RecommendationStatus.Active:
        return 'active';
      case RecommendationStatus.Implemented:
        return 'implemented';
      case RecommendationStatus.Dismissed:
        return 'dismissed';
      default:
        return 'draft';
    }
  }

  priorityClass(r: RecommendationDto): string {
    const p = r.priority;
    if (p >= 7) return 'rec-priority--high';
    if (p >= 4) return 'rec-priority--mid';
    return 'rec-priority--low';
  }

  dateLocale(): string {
    return this.i18n.lang() === 'ar' ? 'ar-SA' : 'en-US';
  }

  cardAccentClass(r: RecommendationDto): string {
    return `rec-card--${this.statusVariant(r)}`;
  }

  descBlock(lang: 'ar' | 'en', r: RecommendationDto): string | null {
    const raw = lang === 'ar' ? r.descriptionAr?.trim() : r.descriptionEn?.trim();
    return raw || null;
  }
}
