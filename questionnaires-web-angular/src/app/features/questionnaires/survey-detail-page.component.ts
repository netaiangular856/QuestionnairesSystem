import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, filter, map } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { SurveyQuestionsApiService } from '../../services/survey-questions-api.service';
import { SurveysApiService } from '../../services/surveys-api.service';
import {
  QuestionDto,
  QuestionType,
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
  imports: [RouterLink, TranslatePipe, FormsModule, DatePipe],
  templateUrl: './survey-detail-page.component.html',
  styleUrl: './survey-detail-page.component.scss',
})
export class SurveyDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly surveysApi = inject(SurveysApiService);
  private readonly surveyQuestionsApi = inject(SurveyQuestionsApiService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  readonly canManage = this.auth.hasPermission(PermissionCodes.SurveyManage);
  readonly canApproveWorkflow = this.auth.hasAnyPermission([
    PermissionCodes.SurveyManage,
    PermissionCodes.SurveyApprove,
  ]);
  readonly canParticipants = this.auth.hasPermission(PermissionCodes.ParticipantView);
  readonly canResponses = this.auth.hasPermission(PermissionCodes.ResponseView);
  readonly canReport = this.auth.hasPermission(PermissionCodes.ReportView);
  readonly canViewQuestions = this.auth.hasPermission(PermissionCodes.QuestionView);

  readonly SurveyStatus = SurveyStatus;
  readonly SurveyAudienceScope = SurveyAudienceScope;
  readonly QuestionType = QuestionType;

  readonly questionTypeLabels: { value: QuestionType; labelKey: string }[] = [
    { value: QuestionType.ShortText, labelKey: 'q.templates.qt.shortText' },
    { value: QuestionType.LongText, labelKey: 'q.templates.qt.longText' },
    { value: QuestionType.SingleChoice, labelKey: 'q.templates.qt.single' },
    { value: QuestionType.MultipleChoice, labelKey: 'q.templates.qt.multi' },
    { value: QuestionType.Rating, labelKey: 'q.templates.qt.rating' },
    { value: QuestionType.Scale, labelKey: 'q.templates.qt.scale' },
    { value: QuestionType.YesNo, labelKey: 'q.templates.qt.yesno' },
    { value: QuestionType.Date, labelKey: 'q.templates.qt.date' },
    { value: QuestionType.Number, labelKey: 'q.templates.qt.number' },
  ];

  readonly survey = signal<SurveyDetailDto | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(true);

  readonly questions = signal<QuestionDto[]>([]);

  readonly analytics = signal<SurveyAnalyticsDto | null>(null);
  readonly analyticsSummary = signal<SurveyAnalyticsSummaryDto | null>(null);
  readonly analyticsBusy = signal(false);

  readonly actionBusy = signal(false);

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
    this.questions.set([]);
    forkJoin({
      survey: this.surveysApi.getById(id),
      questions: this.canViewQuestions
        ? this.surveyQuestionsApi.list(id).pipe(catchError(() => of([] as QuestionDto[])))
        : of([] as QuestionDto[]),
    }).subscribe({
      next: ({ survey, questions }) => {
        this.survey.set(survey);
        if (this.canViewQuestions) {
          const sorted = [...(questions ?? [])].sort((a, b) => a.displayOrder - b.displayOrder);
          this.questions.set(sorted);
        }
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

  hasOverviewSection(s: SurveyDetailDto): boolean {
    return !!(
      s.descriptionAr?.trim() ||
      s.descriptionEn?.trim() ||
      s.opensAtUtc ||
      s.closesAtUtc
    );
  }

  questionTitle(q: QuestionDto): string {
    return qLocalizedTitle(this.i18n.lang(), q.titleAr, q.titleEn);
  }

  questionTypeLabelKey(q: QuestionDto): string {
    return this.questionTypeLabels.find((x) => x.value === q.type)?.labelKey ?? 'q.templates.qt.shortText';
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
    return this.canApproveWorkflow && s.status === SurveyStatus.PendingApproval;
  }

  showPublish(s: SurveyDetailDto): boolean {
    return this.canManage && s.status === SurveyStatus.Approved;
  }

  showClose(s: SurveyDetailDto): boolean {
    return this.canManage && s.status === SurveyStatus.Published;
  }

  openEdit(): void {
    const id = this.currentId();
    if (!id) return;
    void this.router.navigate(['/surveys', id, 'edit']);
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

  /** In-page jumps (TOC). Plain `href="#id"` can break SPA routing or hash handling; scroll + fragment keeps URLs shareable. */
  jumpToSection(elementId: string): void {
    const el = document.getElementById(elementId);
    el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    void this.router.navigate([], {
      relativeTo: this.route,
      fragment: elementId,
      replaceUrl: true,
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
