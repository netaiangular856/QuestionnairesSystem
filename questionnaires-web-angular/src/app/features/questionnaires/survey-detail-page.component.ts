import { DatePipe, DecimalPipe } from '@angular/common';
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
  QuestionAnalyticsDto,
  QuestionDto,
  QuestionType,
  SurveyAudienceScope,
  SurveyComprehensiveAnalyticsDto,
  SurveyDetailDto,
  SurveyStatus,
  UpdateSurveyRequest,
} from '../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { qAudienceKey, qLocalizedTitle, qSurveyStatusKey } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { RecommendationCreatePanelComponent } from './recommendation-create-panel.component';

@Component({
  selector: 'app-survey-detail-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe, FormsModule, DatePipe, DecimalPipe, RecommendationCreatePanelComponent],
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
  readonly canRecommendManage = this.auth.hasPermission(PermissionCodes.RecommendationManage);

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

  readonly comprehensiveAnalytics = signal<SurveyComprehensiveAnalyticsDto | null>(null);
  readonly analyticsBusy = signal(false);

  /** Backend `QuestionType.ToString()` → i18n keys used elsewhere for labels. */
  private readonly backendQuestionTypeKeys: Record<string, string> = {
    ShortText: 'q.templates.qt.shortText',
    LongText: 'q.templates.qt.longText',
    SingleChoice: 'q.templates.qt.single',
    MultipleChoice: 'q.templates.qt.multi',
    Rating: 'q.templates.qt.rating',
    Scale: 'q.templates.qt.scale',
    YesNo: 'q.templates.qt.yesno',
    Date: 'q.templates.qt.date',
    Number: 'q.templates.qt.number',
  };

  readonly actionBusy = signal(false);

  readonly deleteOpen = signal(false);

  readonly rejectOpen = signal(false);
  rejectReason = '';

  readonly patchOpen = signal(false);
  patchStatus: SurveyStatus = SurveyStatus.Draft;

  readonly recommendationModalOpen = signal(false);

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
    this.comprehensiveAnalytics.set(null);
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
    this.surveysApi
      .getComprehensiveAnalytics(surveyId)
      .pipe(catchError(() => of(null as SurveyComprehensiveAnalyticsDto | null)))
      .subscribe({
        next: (data) => {
          this.comprehensiveAnalytics.set(data);
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

  /** Approximate share of answer cells filled vs full matrix (submitted × questions). */
  fillRatePercent(ca: SurveyComprehensiveAnalyticsDto): number {
    const sub = ca.overview.submittedResponses;
    const nq = ca.overview.totalQuestions;
    if (sub <= 0 || nq <= 0) return 0;
    const sumCells = ca.questions.reduce((acc, q) => acc + q.totalAnswers, 0);
    const denom = sub * nq;
    return denom <= 0 ? 0 : Math.min(100, Math.round((100 * sumCells) / denom));
  }

  last30DaysTotal(ca: SurveyComprehensiveAnalyticsDto): number {
    return ca.responseTimeline.reduce((a, t) => a + t.responseCount, 0);
  }

  /**
   * Line + area geometry for submission activity (time series — avoids column/bar semantics).
   */
  timelineLineGeometry(ca: SurveyComprehensiveAnalyticsDto): {
    gradientId: string;
    areaPath: string;
    linePoints: string;
    dots: { cx: number; cy: number; count: number; iso: string }[];
    axisDates: { iso: string }[];
  } | null {
    const tl = ca.responseTimeline;
    if (tl.length === 0) return null;

    const gradientId = `tlg-${ca.surveyId.replace(/[^a-zA-Z0-9_-]/g, '')}`;
    const n = tl.length;
    const max = Math.max(1, ...tl.map((t) => t.responseCount));

    const W = 100;
    const H = 44;
    const padL = 4;
    const padR = 4;
    const padT = 6;
    const padB = 2;
    const plotW = W - padL - padR;
    const plotH = H - padT - padB;

    const xAt = (i: number) => padL + (n <= 1 ? plotW / 2 : (i / (n - 1)) * plotW);
    const yAt = (c: number) => padT + plotH - (c / max) * plotH;

    const dots = tl.map((t, i) => ({
      cx: xAt(i),
      cy: yAt(t.responseCount),
      count: t.responseCount,
      iso: t.date,
    }));

    const linePoints = dots.map((d) => `${d.cx.toFixed(3)},${d.cy.toFixed(3)}`).join(' ');
    const yBottom = padT + plotH;
    const areaPath =
      `M ${dots[0].cx} ${yBottom} ` + dots.map((d) => `L ${d.cx} ${d.cy}`).join(' ') + ` L ${dots[dots.length - 1].cx} ${yBottom} Z`;

    const axisDates: { iso: string }[] = [{ iso: tl[0].date }];
    if (n >= 4) axisDates.push({ iso: tl[Math.floor((n - 1) / 2)].date });
    if (n >= 2) axisDates.push({ iso: tl[n - 1].date });

    const seen = new Set<string>();
    const axisUnique = axisDates.filter((d) => {
      if (seen.has(d.iso)) return false;
      seen.add(d.iso);
      return true;
    });

    return {
      gradientId,
      areaPath,
      linePoints,
      dots,
      axisDates: axisUnique,
    };
  }

  topQuestions(ca: SurveyComprehensiveAnalyticsDto, limit = 5): QuestionAnalyticsDto[] {
    return [...ca.questions].sort((a, b) => b.totalAnswers - a.totalAnswers).slice(0, limit);
  }

  /** Top questions plus share of all recorded answers (clearer than raw counts alone). */
  topQuestionsWithShare(
    ca: SurveyComprehensiveAnalyticsDto,
    limit = 5,
  ): { question: QuestionAnalyticsDto; shareOfAllAnswers: number }[] {
    const tops = this.topQuestions(ca, limit);
    const total = ca.questions.reduce((a, q) => a + q.totalAnswers, 0);
    if (total <= 0) return tops.map((q) => ({ question: q, shareOfAllAnswers: 0 }));
    return tops.map((q) => ({
      question: q,
      shareOfAllAnswers: Math.min(100, Math.round((100 * q.totalAnswers) / total)),
    }));
  }

  insightQuestionTitle(q: QuestionAnalyticsDto): string {
    return qLocalizedTitle(this.i18n.lang(), q.titleAr, q.titleEn);
  }

  insightQuestionTypeKey(q: QuestionAnalyticsDto): string {
    return this.backendQuestionTypeKeys[q.questionType] ?? 'q.templates.qt.shortText';
  }

  categoryTypeLabelKey(name: string): string {
    return this.backendQuestionTypeKeys[name] ?? 'q.detail.table.typeCol';
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

  openRecommendationModal(): void {
    this.recommendationModalOpen.set(true);
  }

  closeRecommendationModal(): void {
    this.recommendationModalOpen.set(false);
  }

  onRecommendationCreatedFromModal(): void {
    this.recommendationModalOpen.set(false);
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
