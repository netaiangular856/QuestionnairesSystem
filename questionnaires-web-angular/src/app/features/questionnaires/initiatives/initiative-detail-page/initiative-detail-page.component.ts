import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { ActionPlansApiService } from '../../../../services/action-plans-api.service';
import { InitiativesApiService } from '../../../../services/initiatives-api.service';
import { QuestionnaireLookupsApiService } from '../../../../services/questionnaire-lookups-api.service';
import {
  AddInitiativeProgressRequest,
  InitiativeDto,
  InitiativeProgressDto,
  InitiativeStatus,
  LookupItemDto,
  UpdateInitiativeRequest,
} from '../../../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../../../shared/models/permission-codes';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';
import { qInitiativeStatusKey, qLocalizedTitle } from '../../../../shared/questionnaires/q-display';
import { ApiBusinessError } from '../../../../shared/utils/api-helpers';

@Component({
  selector: 'app-initiative-detail-page',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe, DatePipe],
  templateUrl: './initiative-detail-page.component.html',
  styleUrl: './initiative-detail-page.component.scss',
})
export class InitiativeDetailPageComponent implements OnInit {
  private readonly initiativesApi = inject(InitiativesApiService);
  private readonly plansApi = inject(ActionPlansApiService);
  private readonly lookupsApi = inject(QuestionnaireLookupsApiService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  readonly InitiativeStatus = InitiativeStatus;

  readonly canManage = () => this.auth.hasPermission(PermissionCodes.ActionPlanManage);

  readonly users = signal<LookupItemDto[]>([]);
  readonly progressRows = signal<InitiativeProgressDto[]>([]);

  readonly lookupsBusy = signal(true);
  readonly detailBusy = signal(true);
  readonly loadFailed = signal(false);
  readonly saveBusy = signal(false);
  readonly progressBusy = signal(false);

  readonly editModalOpen = signal(false);
  readonly progressModalOpen = signal(false);

  initiativeId = '';
  actionPlanId = '';
  planHeadline = '';

  /** Last loaded initiative — form is reset from this whenever edit opens */
  private lastLoadedInitiative: InitiativeDto | null = null;

  titleAr = '';
  titleEn = '';
  descriptionAr = '';
  descriptionEn = '';
  status: InitiativeStatus = InitiativeStatus.Planned;
  ownerUserId: string | null = null;
  targetDate = '';

  progressPercent: number | null = null;
  progressNotes = '';

  readonly sortedProgress = computed<InitiativeProgressDto[]>(() => {
    const rows = [...this.progressRows()];
    rows.sort((a, b) => new Date(b.recordedAtUtc).getTime() - new Date(a.recordedAtUtc).getTime());
    return rows;
  });

  readonly ringCircumference = 2 * Math.PI * 52;

  ngOnInit(): void {
    this.lookupsApi.getUsers('', 500).subscribe({
      next: (users) => {
        this.users.set(users);
        this.lookupsBusy.set(false);
      },
      error: () => this.lookupsBusy.set(false),
    });

    this.route.paramMap.subscribe((pm) => {
      const id = pm.get('initiativeId');
      if (!id) return;
      this.initiativeId = id;
      this.load(id);
    });
  }

  load(id: string): void {
    this.detailBusy.set(true);
    this.loadFailed.set(false);
    this.initiativesApi.getById(id).subscribe({
      next: (init) => {
        this.lastLoadedInitiative = init;
        this.patchFromInitiative(init);
        this.actionPlanId = init.actionPlanId;
        this.plansApi.getById(init.actionPlanId).subscribe({
          next: (plan) => {
            this.planHeadline = qLocalizedTitle(this.i18n.lang(), plan.titleAr, plan.titleEn);
          },
          error: () => {
            this.planHeadline = '';
          },
        });
        this.initiativesApi.listProgress(id).subscribe({
          next: (rows) => this.progressRows.set(rows),
          error: () => this.progressRows.set([]),
        });
        this.detailBusy.set(false);
      },
      error: () => {
        this.loadFailed.set(true);
        this.detailBusy.set(false);
      },
    });
  }

  private patchFromInitiative(i: InitiativeDto): void {
    this.titleAr = i.titleAr;
    this.titleEn = i.titleEn;
    this.descriptionAr = i.descriptionAr ?? '';
    this.descriptionEn = i.descriptionEn ?? '';
    this.status = i.status;
    this.ownerUserId = i.ownerUserId;
    this.targetDate = this.isoToDateInput(i.targetDateUtc);
  }

  private isoToDateInput(iso: string | null | undefined): string {
    if (!iso) return '';
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return '';
    return d.toISOString().slice(0, 10);
  }

  private dateToUtc(s: string): string | null {
    const t = s?.trim();
    if (!t) return null;
    const d = new Date(`${t}T12:00:00.000Z`);
    return Number.isNaN(d.getTime()) ? null : d.toISOString();
  }

  headline(): string {
    return qLocalizedTitle(this.i18n.lang(), this.titleAr, this.titleEn);
  }

  descDisplay(): string {
    const ar = this.descriptionAr?.trim() ?? '';
    const en = this.descriptionEn?.trim() ?? '';
    if (this.i18n.lang() === 'ar') return ar || en;
    return en || ar;
  }

  statusLabel(): string {
    return qInitiativeStatusKey(this.status);
  }

  statusVariant(): 'draft' | 'active' | 'implemented' | 'risk' | 'dismissed' {
    switch (this.status) {
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

  userLabel(u: LookupItemDto): string {
    const n = u.name?.trim() || '—';
    const e = u.email?.trim();
    return e ? `${n} (${e})` : n;
  }

  dateLocale(): string {
    return this.i18n.lang() === 'ar' ? 'ar-SA' : 'en-US';
  }

  /** Most recent non-null progress percent, or null. */
  latestProgressPercent(): number | null {
    for (const r of this.sortedProgress()) {
      if (r.progressPercent != null) return r.progressPercent;
    }
    return null;
  }

  /** Difference (percentage points) between the two most-recent numeric percentages.
   * Positive = improvement. Null if not enough data. */
  progressDelta(): number | null {
    const nums: number[] = [];
    for (const r of this.sortedProgress()) {
      if (r.progressPercent != null) nums.push(r.progressPercent);
      if (nums.length === 2) break;
    }
    if (nums.length < 2) return null;
    return nums[0] - nums[1];
  }

  lastUpdateIso(): string | null {
    const rows = this.sortedProgress();
    return rows.length > 0 ? rows[0].recordedAtUtc : null;
  }

  ringDashOffset(pct: number | null): number {
    const c = this.ringCircumference;
    if (pct == null) return c;
    const clamped = Math.max(0, Math.min(100, pct));
    return c * (1 - clamped / 100);
  }

  ringToneVar(): string {
    const pct = this.latestProgressPercent();
    if (pct == null) return 'var(--init-ring-neutral)';
    if (pct >= 75) return 'var(--init-ring-strong)';
    if (pct >= 40) return 'var(--init-ring-mid)';
    return 'var(--init-ring-weak)';
  }

  daysToTarget(): number | null {
    if (!this.targetDate) return null;
    const target = new Date(`${this.targetDate}T12:00:00.000Z`).getTime();
    if (!isFinite(target)) return null;
    return Math.round((target - Date.now()) / (1000 * 60 * 60 * 24));
  }

  ownerInitial(): string {
    const u = this.users().find((x) => x.id === this.ownerUserId);
    const name = u?.name?.trim();
    if (!name) return '';
    return name.charAt(0).toLocaleUpperCase(this.i18n.lang() === 'ar' ? 'ar' : 'en');
  }

  ownerDisplayName(): string {
    const u = this.users().find((x) => x.id === this.ownerUserId);
    return u?.name?.trim() || '';
  }

  openEditModal(): void {
    if (this.lastLoadedInitiative) {
      this.patchFromInitiative(this.lastLoadedInitiative);
    }
    this.editModalOpen.set(true);
  }

  closeEditModal(): void {
    this.editModalOpen.set(false);
  }

  openProgressModal(): void {
    this.progressPercent = this.latestProgressPercent() ?? 0;
    this.progressNotes = '';
    this.progressModalOpen.set(true);
  }

  closeProgressModal(): void {
    this.progressModalOpen.set(false);
  }

  save(): void {
    const tAr = this.titleAr.trim();
    const tEn = this.titleEn.trim();
    if (!tAr || !tEn) {
      this.toast.show(this.i18n.t('q.plans.validationTitles'), 'error');
      return;
    }
    const body: UpdateInitiativeRequest = {
      titleAr: tAr,
      titleEn: tEn,
      descriptionAr: this.descriptionAr.trim() || null,
      descriptionEn: this.descriptionEn.trim() || null,
      status: this.status,
      ownerUserId: this.ownerUserId,
      targetDateUtc: this.dateToUtc(this.targetDate),
    };
    this.saveBusy.set(true);
    this.initiativesApi.update(this.initiativeId, body).subscribe({
      next: () => {
        this.saveBusy.set(false);
        this.toast.show(this.i18n.t('q.initiative.saveSuccess'), 'success');
        this.closeEditModal();
        this.load(this.initiativeId);
      },
      error: (err: unknown) => {
        this.saveBusy.set(false);
        const msg =
          err instanceof ApiBusinessError && err.errors.length > 0
            ? err.errors[0]
            : this.i18n.t('q.initiative.saveError');
        this.toast.show(msg, 'error');
      },
    });
  }

  submitProgress(): void {
    const body: AddInitiativeProgressRequest = {
      progressPercent: this.progressPercent,
      notes: this.progressNotes.trim() || null,
    };
    this.progressBusy.set(true);
    this.initiativesApi.addProgress(this.initiativeId, body).subscribe({
      next: () => {
        this.progressBusy.set(false);
        this.progressPercent = null;
        this.progressNotes = '';
        this.toast.show(this.i18n.t('q.initiative.progressSuccess'), 'success');
        this.closeProgressModal();
        this.load(this.initiativeId);
      },
      error: (err: unknown) => {
        this.progressBusy.set(false);
        const msg =
          err instanceof ApiBusinessError && err.errors.length > 0
            ? err.errors[0]
            : this.i18n.t('q.initiative.progressError');
        this.toast.show(msg, 'error');
      },
    });
  }
}
