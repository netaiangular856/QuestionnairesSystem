import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, filter, map } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { SurveysApiService } from '../../services/surveys-api.service';
import {
  SurveyAnalyticsDto,
  SurveyAnalyticsSummaryDto,
  SurveyAudienceScope,
  SurveyDetailDto,
  SurveyStatus,
  UpdateSurveyRequest,
} from '../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { qAudienceKey, qLocalizedTitle, qSurveyStatusKey } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

@Component({
  selector: 'app-survey-detail-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe, FormsModule],
  templateUrl: './survey-detail-page.component.html',
  styleUrl: './survey-detail-page.component.scss',
})
export class SurveyDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly surveysApi = inject(SurveysApiService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  readonly canManage = this.auth.hasPermission(PermissionCodes.SurveyManage);
  readonly canParticipants = this.auth.hasPermission(PermissionCodes.ParticipantView);
  readonly canResponses = this.auth.hasPermission(PermissionCodes.ResponseView);
  readonly canReport = this.auth.hasPermission(PermissionCodes.ReportView);

  readonly SurveyStatus = SurveyStatus;
  readonly SurveyAudienceScope = SurveyAudienceScope;

  readonly survey = signal<SurveyDetailDto | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(true);

  readonly analytics = signal<SurveyAnalyticsDto | null>(null);
  readonly analyticsSummary = signal<SurveyAnalyticsSummaryDto | null>(null);
  readonly analyticsBusy = signal(false);

  readonly actionBusy = signal(false);

  readonly editOpen = signal(false);
  editModel: UpdateSurveyRequest = {
    titleAr: '',
    titleEn: '',
    descriptionAr: '',
    descriptionEn: '',
    code: '',
    audienceScope: SurveyAudienceScope.AllOrganizationMembers,
  };

  readonly deleteOpen = signal(false);

  readonly rejectOpen = signal(false);
  rejectReason = '';

  readonly patchOpen = signal(false);
  patchStatus: SurveyStatus = SurveyStatus.Draft;

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        map((pm) => pm.get('surveyId')),
        filter((id): id is string => !!id),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((id) => this.loadSurvey(id));
  }

  private loadSurvey(id: string): void {
    this.busy.set(true);
    this.failed.set(false);
    this.analytics.set(null);
    this.analyticsSummary.set(null);
    this.surveysApi.getById(id).subscribe({
      next: (s) => {
        this.survey.set(s);
        this.busy.set(false);
        if (this.canReport) this.loadAnalytics(id);
      },
      error: () => {
        this.failed.set(true);
        this.busy.set(false);
      },
    });
  }

  private loadAnalytics(surveyId: string): void {
    this.analyticsBusy.set(true);
    forkJoin({
      main: this.surveysApi.getAnalytics(surveyId).pipe(catchError(() => of(null as SurveyAnalyticsDto | null))),
      summary: this.surveysApi
        .getAnalyticsSummary(surveyId)
        .pipe(catchError(() => of(null as SurveyAnalyticsSummaryDto | null))),
    }).subscribe({
      next: ({ main, summary }) => {
        this.analytics.set(main);
        this.analyticsSummary.set(summary);
        this.analyticsBusy.set(false);
      },
      error: () => {
        this.analyticsBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.analytics.error'), 'error');
      },
    });
  }

  private currentId(): string | null {
    return this.route.snapshot.paramMap.get('surveyId');
  }

  titleOf(s: SurveyDetailDto): string {
    return qLocalizedTitle(this.i18n.lang(), s.titleAr, s.titleEn);
  }

  statusKey(s: SurveyDetailDto): string {
    return qSurveyStatusKey(s.status);
  }

  audienceKey(s: SurveyDetailDto): string {
    return qAudienceKey(s.audienceScope);
  }

  /** Stacked strip: submitted (of total responses) */
  mixSubmittedPct(an: SurveyAnalyticsDto): number {
    const t = an.totalResponses;
    if (t <= 0) return 0;
    return Math.min(100, (100 * an.submittedResponses) / t);
  }

  mixInProgressPct(an: SurveyAnalyticsDto): number {
    const t = an.totalResponses;
    if (t <= 0) return 0;
    return Math.min(100, (100 * an.inProgressResponses) / t);
  }

  mixOtherPct(an: SurveyAnalyticsDto): number {
    const t = an.totalResponses;
    if (t <= 0) return 0;
    const other = Math.max(0, t - an.submittedResponses - an.inProgressResponses);
    return (100 * other) / t;
  }

  /** Pie chart fill (no numeric labels in UI; proportions only). */
  pieGradient(an: SurveyAnalyticsDto): string {
    const s = this.mixSubmittedPct(an);
    const p = this.mixInProgressPct(an);
    const a = s * 3.6;
    const b = (s + p) * 3.6;
    return `conic-gradient(#166534 0deg ${a}deg, #ca8a04 ${a}deg ${b}deg, #78716c ${b}deg 360deg)`;
  }

  showSubmit(s: SurveyDetailDto): boolean {
    return this.canManage && (s.status === SurveyStatus.Draft || s.status === SurveyStatus.Rejected);
  }

  showApproveReject(s: SurveyDetailDto): boolean {
    return this.canManage && s.status === SurveyStatus.PendingApproval;
  }

  showPublish(s: SurveyDetailDto): boolean {
    return this.canManage && s.status === SurveyStatus.Approved;
  }

  showClose(s: SurveyDetailDto): boolean {
    return this.canManage && s.status === SurveyStatus.Published;
  }

  openEdit(): void {
    const s = this.survey();
    if (!s) return;
    this.editModel = {
      titleAr: s.titleAr,
      titleEn: s.titleEn,
      descriptionAr: s.descriptionAr ?? '',
      descriptionEn: s.descriptionEn ?? '',
      code: s.code ?? '',
      audienceScope: s.audienceScope,
    };
    this.editOpen.set(true);
  }

  saveEdit(): void {
    const id = this.currentId();
    if (!id || !this.editModel.titleAr.trim() || !this.editModel.titleEn.trim()) {
      this.toast.show(this.i18n.t('users.toast.editRequired'), 'error');
      return;
    }
    this.actionBusy.set(true);
    this.surveysApi
      .update(id, {
        ...this.editModel,
        titleAr: this.editModel.titleAr.trim(),
        titleEn: this.editModel.titleEn.trim(),
        descriptionAr: this.editModel.descriptionAr?.trim() || null,
        descriptionEn: this.editModel.descriptionEn?.trim() || null,
        code: this.editModel.code?.trim() || null,
      })
      .subscribe({
        next: (s) => {
          this.survey.set(s);
          this.editOpen.set(false);
          this.actionBusy.set(false);
          this.toast.show(this.i18n.t('q.detail.toast.updated'), 'success');
          if (this.canReport) this.loadAnalytics(id);
        },
        error: () => {
          this.actionBusy.set(false);
          this.toast.show(this.i18n.t('q.detail.toast.updateFailed'), 'error');
        },
      });
  }

  openDelete(): void {
    this.deleteOpen.set(true);
  }

  confirmDelete(): void {
    const id = this.currentId();
    if (!id) return;
    this.actionBusy.set(true);
    this.surveysApi.delete(id).subscribe({
      next: () => {
        this.actionBusy.set(false);
        this.deleteOpen.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.deleted'), 'success');
        void this.router.navigate(['/surveys']);
      },
      error: () => {
        this.actionBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.deleteFailed'), 'error');
      },
    });
  }

  duplicate(): void {
    const id = this.currentId();
    if (!id) return;
    this.actionBusy.set(true);
    this.surveysApi.duplicate(id).subscribe({
      next: (s) => {
        this.actionBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.duplicated'), 'success');
        void this.router.navigate(['/surveys', s.id]);
      },
      error: () => {
        this.actionBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.duplicateFailed'), 'error');
      },
    });
  }

  submitForApproval(): void {
    const id = this.currentId();
    if (!id) return;
    this.runTransition(() => this.surveysApi.submitForApproval(id));
  }

  approve(): void {
    const id = this.currentId();
    if (!id) return;
    this.runTransition(() => this.surveysApi.approve(id));
  }

  openReject(): void {
    this.rejectReason = '';
    this.rejectOpen.set(true);
  }

  submitReject(): void {
    const id = this.currentId();
    if (!id || !this.rejectReason.trim()) {
      this.toast.show(this.i18n.t('q.detail.toast.rejectReason'), 'error');
      return;
    }
    this.actionBusy.set(true);
    this.surveysApi.reject(id, { reason: this.rejectReason.trim() }).subscribe({
      next: (s) => {
        this.survey.set(s);
        this.rejectOpen.set(false);
        this.actionBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.transitionOk'), 'success');
        if (this.canReport) this.loadAnalytics(id);
      },
      error: () => {
        this.actionBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.transitionFailed'), 'error');
      },
    });
  }

  publish(): void {
    const id = this.currentId();
    if (!id) return;
    this.runTransition(() => this.surveysApi.publish(id));
  }

  closeSurvey(): void {
    const id = this.currentId();
    if (!id) return;
    this.runTransition(() => this.surveysApi.close(id));
  }

  openPatch(): void {
    const s = this.survey();
    this.patchStatus = s?.status ?? SurveyStatus.Draft;
    this.patchOpen.set(true);
  }

  applyPatch(): void {
    const id = this.currentId();
    if (!id) return;
    this.actionBusy.set(true);
    this.surveysApi.patchStatus(id, { status: this.patchStatus }).subscribe({
      next: (s) => {
        this.survey.set(s);
        this.patchOpen.set(false);
        this.actionBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.transitionOk'), 'success');
        if (this.canReport) this.loadAnalytics(id);
      },
      error: () => {
        this.actionBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.transitionFailed'), 'error');
      },
    });
  }

  private runTransition(req: () => Observable<SurveyDetailDto>): void {
    this.actionBusy.set(true);
    req().subscribe({
      next: (s) => {
        const id = this.currentId();
        this.survey.set(s);
        this.actionBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.transitionOk'), 'success');
        if (id && this.canReport) this.loadAnalytics(id);
      },
      error: () => {
        this.actionBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.transitionFailed'), 'error');
      },
    });
  }
}
