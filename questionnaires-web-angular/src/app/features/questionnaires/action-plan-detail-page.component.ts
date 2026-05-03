import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { ActionPlansApiService } from '../../services/action-plans-api.service';
import { QuestionnaireLookupsApiService } from '../../services/questionnaire-lookups-api.service';
import {
  ActionPlanDto,
  ActionPlanStatus,
  CreateInitiativeRequest,
  InitiativeDto,
  LookupItemDto,
  UpdateActionPlanRequest,
} from '../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { qInitiativeStatusKey, qLocalizedTitle, qPlanStatusKey } from '../../shared/questionnaires/q-display';
import { ApiBusinessError } from '../../shared/utils/api-helpers';
import { openDatePicker as openNativeDatePicker } from '../../shared/utils/open-datetime-local-picker';

interface PlanFormDraft {
  titleAr: string;
  titleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  surveyId: string | null;
  ownerUserId: string | null;
  status: ActionPlanStatus;
  startDate: string;
  endDate: string;
}

@Component({
  selector: 'app-action-plan-detail-page',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe, DatePipe],
  providers: [DatePipe],
  templateUrl: './action-plan-detail-page.component.html',
  styleUrl: './action-plan-detail-page.component.scss',
})
export class ActionPlanDetailPageComponent implements OnInit {
  private readonly api = inject(ActionPlansApiService);
  private readonly lookupsApi = inject(QuestionnaireLookupsApiService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly datePipe = inject(DatePipe);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  readonly openDatePicker = openNativeDatePicker;

  readonly ActionPlanStatus = ActionPlanStatus;

  readonly canManage = () => this.auth.hasPermission(PermissionCodes.ActionPlanManage);

  readonly surveys = signal<LookupItemDto[]>([]);
  readonly users = signal<LookupItemDto[]>([]);
  readonly initiatives = signal<InitiativeDto[]>([]);
  readonly lookupsBusy = signal(true);
  readonly detailBusy = signal(true);
  readonly loadFailed = signal(false);
  readonly saveBusy = signal(false);
  readonly addInitBusy = signal(false);
  readonly editPlanModalOpen = signal(false);
  readonly addInitModalOpen = signal(false);

  private planFormDraft: PlanFormDraft | null = null;

  /** Current route plan id */
  planId = '';

  titleAr = '';
  titleEn = '';
  descriptionAr = '';
  descriptionEn = '';
  surveyId: string | null = null;
  ownerUserId: string | null = null;
  status: ActionPlanStatus = ActionPlanStatus.Draft;
  startDate = '';
  endDate = '';

  initTitleAr = '';
  initTitleEn = '';
  initDescriptionAr = '';
  initDescriptionEn = '';
  initOwnerUserId: string | null = null;
  initTargetDate = '';

  ngOnInit(): void {
    forkJoin({
      surveys: this.lookupsApi.getSurveys('', 500).pipe(catchError(() => of([] as LookupItemDto[]))),
      users: this.lookupsApi.getUsers('', 500).pipe(catchError(() => of([] as LookupItemDto[]))),
    }).subscribe({
      next: ({ surveys, users }) => {
        this.surveys.set(surveys);
        this.users.set(users);
        this.lookupsBusy.set(false);
      },
      error: () => this.lookupsBusy.set(false),
    });

    this.route.paramMap.subscribe((pm) => {
      const id = pm.get('planId');
      if (!id?.trim()) {
        this.detailBusy.set(false);
        this.loadFailed.set(true);
        return;
      }
      this.planId = id;
      this.loadPlan(id);
    });
  }

  loadPlan(id: string): void {
    this.detailBusy.set(true);
    this.loadFailed.set(false);
    forkJoin({
      plan: this.api.getById(id),
      initiatives: this.api.listInitiatives(id).pipe(catchError(() => of([] as InitiativeDto[]))),
    }).subscribe({
      next: ({ plan, initiatives }) => {
        this.patchFromPlan(plan);
        this.initiatives.set(initiatives);
        this.detailBusy.set(false);
      },
      error: () => {
        this.loadFailed.set(true);
        this.detailBusy.set(false);
      },
    });
  }

  private patchFromPlan(p: ActionPlanDto): void {
    this.titleAr = p.titleAr ?? '';
    this.titleEn = p.titleEn ?? '';
    this.descriptionAr = p.descriptionAr ?? '';
    this.descriptionEn = p.descriptionEn ?? '';
    this.surveyId = p.surveyId ?? null;
    this.ownerUserId = p.ownerUserId ?? null;
    this.status = p.status ?? ActionPlanStatus.Draft;
    this.startDate = this.isoToDateInput(p.startDateUtc);
    this.endDate = this.isoToDateInput(p.endDateUtc);
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

  planStatusLabel(): string {
    return qPlanStatusKey(this.status);
  }

  initiativeStatusKey(i: InitiativeDto): string {
    return qInitiativeStatusKey(i.status);
  }

  initiativeTitle(i: InitiativeDto): string {
    return qLocalizedTitle(this.i18n.lang(), i.titleAr, i.titleEn);
  }

  surveyLabel(s: LookupItemDto): string {
    return s.name?.trim() || '—';
  }

  userLabel(u: LookupItemDto): string {
    const n = u.name?.trim() || '—';
    const e = u.email?.trim();
    return e ? `${n} (${e})` : n;
  }

  dateLabel(isoDate: string): string {
    const t = isoDate?.trim();
    if (!t) return '—';
    const d = new Date(`${t}T12:00:00`);
    return Number.isNaN(d.getTime()) ? '—' : (this.datePipe.transform(d, 'mediumDate') ?? '—');
  }

  surveyDisplay(): string {
    if (!this.surveyId) return '—';
    const s = this.surveys().find((x) => x.id === this.surveyId);
    return s ? this.surveyLabel(s) : '—';
  }

  ownerDisplay(): string {
    if (!this.ownerUserId) return '—';
    const u = this.users().find((x) => x.id === this.ownerUserId);
    return u ? this.userLabel(u) : '—';
  }

  openEditPlanModal(): void {
    this.planFormDraft = {
      titleAr: this.titleAr,
      titleEn: this.titleEn,
      descriptionAr: this.descriptionAr,
      descriptionEn: this.descriptionEn,
      surveyId: this.surveyId,
      ownerUserId: this.ownerUserId,
      status: this.status,
      startDate: this.startDate,
      endDate: this.endDate,
    };
    this.editPlanModalOpen.set(true);
  }

  closeEditPlanModal(): void {
    if (this.saveBusy()) return;
    const d = this.planFormDraft;
    if (d) {
      this.titleAr = d.titleAr;
      this.titleEn = d.titleEn;
      this.descriptionAr = d.descriptionAr;
      this.descriptionEn = d.descriptionEn;
      this.surveyId = d.surveyId;
      this.ownerUserId = d.ownerUserId;
      this.status = d.status;
      this.startDate = d.startDate;
      this.endDate = d.endDate;
    }
    this.planFormDraft = null;
    this.editPlanModalOpen.set(false);
  }

  openAddInitModal(): void {
    this.addInitModalOpen.set(true);
  }

  closeAddInitModal(): void {
    if (this.addInitBusy()) return;
    this.initTitleAr = '';
    this.initTitleEn = '';
    this.initDescriptionAr = '';
    this.initDescriptionEn = '';
    this.initOwnerUserId = null;
    this.initTargetDate = '';
    this.addInitModalOpen.set(false);
  }

  savePlan(): void {
    const tAr = this.titleAr.trim();
    const tEn = this.titleEn.trim();
    if (!tAr || !tEn) {
      this.toast.show(this.i18n.t('q.plans.validationTitles'), 'error');
      return;
    }
    const body: UpdateActionPlanRequest = {
      titleAr: tAr,
      titleEn: tEn,
      descriptionAr: this.descriptionAr.trim() || null,
      descriptionEn: this.descriptionEn.trim() || null,
      surveyId: this.surveyId,
      ownerUserId: this.ownerUserId,
      status: this.status,
      startDateUtc: this.dateToUtc(this.startDate),
      endDateUtc: this.dateToUtc(this.endDate),
    };
    this.saveBusy.set(true);
    this.api.update(this.planId, body).subscribe({
      next: () => {
        this.saveBusy.set(false);
        this.planFormDraft = null;
        this.editPlanModalOpen.set(false);
        this.toast.show(this.i18n.t('q.plans.saveSuccess'), 'success');
        this.loadPlan(this.planId);
      },
      error: (err: unknown) => {
        this.saveBusy.set(false);
        const msg =
          err instanceof ApiBusinessError && err.errors.length > 0
            ? err.errors[0]
            : this.i18n.t('q.plans.saveError');
        this.toast.show(msg, 'error');
      },
    });
  }

  addInitiative(): void {
    const tAr = this.initTitleAr.trim();
    const tEn = this.initTitleEn.trim();
    if (!tAr || !tEn) {
      this.toast.show(this.i18n.t('q.plans.validationTitles'), 'error');
      return;
    }
    const body: CreateInitiativeRequest = {
      titleAr: tAr,
      titleEn: tEn,
      descriptionAr: this.initDescriptionAr.trim() || null,
      descriptionEn: this.initDescriptionEn.trim() || null,
      ownerUserId: this.initOwnerUserId,
      targetDateUtc: this.dateToUtc(this.initTargetDate),
    };
    this.addInitBusy.set(true);
    this.api.addInitiative(this.planId, body).subscribe({
      next: () => {
        this.addInitBusy.set(false);
        this.initTitleAr = '';
        this.initTitleEn = '';
        this.initDescriptionAr = '';
        this.initDescriptionEn = '';
        this.initOwnerUserId = null;
        this.initTargetDate = '';
        this.addInitModalOpen.set(false);
        this.toast.show(this.i18n.t('q.plans.addInitSuccess'), 'success');
        this.loadPlan(this.planId);
      },
      error: (err: unknown) => {
        this.addInitBusy.set(false);
        const msg =
          err instanceof ApiBusinessError && err.errors.length > 0
            ? err.errors[0]
            : this.i18n.t('q.plans.addInitError');
        this.toast.show(msg, 'error');
      },
    });
  }
}
