import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { Chart, ChartData, ChartOptions, TooltipOptions, registerables } from 'chart.js';
import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { AiApiService } from '../../../../services/ai-api.service';
import { QuestionnaireLookupsApiService } from '../../../../services/questionnaire-lookups-api.service';
import {
  AiAnalyzeReportsResponseDto,
  AiSentimentAnalysisResponseDto,
  AiChartSuggestionDto,
  AiSuggestActionPlanDraftDto,
  AiSuggestRecommendationDraftDto,
  CrossSurveyAnalyticsFilterRequest,
  LookupItemDto,
} from '../../../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../../../shared/models/permission-codes';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';
import { ApiBusinessError } from '../../../../shared/utils/api-helpers';
import {
  buildSentimentInsightChartData,
  sentimentMixDoughnutColors,
} from '../../../../shared/charts/sentiment-chart-colors';

Chart.register(...registerables);

type AiScopeRunKind = 'idle' | 'analyze' | 'sentiment' | 'recommendation' | 'actionPlan';

@Component({
  selector: 'app-ai-scope-actions-page',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe, BaseChartDirective],
  templateUrl: './ai-scope-actions-page.component.html',
  styleUrl: './ai-scope-actions-page.component.scss',
})
export class AiScopeActionsPageComponent implements OnInit {
  /** Palette tuned for light backgrounds */
  private static readonly ChartPalette = ['#6366f1', '#38bdf8', '#22c55e', '#f59e0b', '#ec4899', '#8b5cf6', '#06b6d4'];

  private readonly aiApi = inject(AiApiService);
  private readonly lookupsApi = inject(QuestionnaireLookupsApiService);
  private readonly toast = inject(ToastService);
  private readonly sanitizer = inject(DomSanitizer);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  readonly canUseAi = this.auth.hasPermission(PermissionCodes.ReportView);

  readonly surveyList = signal<LookupItemDto[]>([]);
  /** Which AI card is running (spinner on that button only; no full-page banner). */
  readonly scopeRun = signal<AiScopeRunKind>('idle');
  readonly busy = computed(() => this.scopeRun() !== 'idle');
  readonly errorText = signal('');

  fromDate = '';
  toDate = '';
  selectedSurveyId = '';

  readonly lastAnalyze = signal<AiAnalyzeReportsResponseDto | null>(null);
  readonly lastSentiment = signal<AiSentimentAnalysisResponseDto | null>(null);
  readonly lastRecommendation = signal<AiSuggestRecommendationDraftDto | null>(null);
  readonly lastActionPlan = signal<AiSuggestActionPlanDraftDto | null>(null);

  readonly analyzeSummarySafe = computed<SafeHtml>(() => {
    const r = this.lastAnalyze();
    if (!r) return '';
    const raw = this.i18n.lang() === 'ar' ? r.summaryAr : r.summaryEn;
    return this.sanitizer.bypassSecurityTrustHtml(raw ?? '');
  });

  readonly sentimentSummarySafe = computed<SafeHtml>(() => {
    const s = this.lastSentiment();
    if (!s) return '';
    const raw = this.i18n.lang() === 'ar' ? s.summaryAr : s.summaryEn;
    return this.sanitizer.bypassSecurityTrustHtml(raw ?? '');
  });

  readonly sentimentDoughnutData = computed<ChartData<'doughnut'> | null>(() => {
    const s = this.lastSentiment();
    const m = s?.sentimentMix;
    if (!m) return null;
    const pos = Number(m.positive) || 0;
    const neg = Number(m.negative) || 0;
    const neu = Number(m.neutral) || 0;
    if (pos + neg + neu <= 0) return null;
    this.i18n.lang();
    const labels = [
      this.i18n.t('q.aiScope.sentiment.positive'),
      this.i18n.t('q.aiScope.sentiment.negative'),
      this.i18n.t('q.aiScope.sentiment.neutral'),
    ];
    const { backgroundColor, borderColor } = sentimentMixDoughnutColors();
    return {
      labels,
      datasets: [
        {
          data: [pos, neg, neu],
          label: this.i18n.t('q.aiScope.sentiment.chartTitle'),
          backgroundColor,
          borderColor,
          borderWidth: 2,
        },
      ],
    };
  });

  readonly chartOptions = computed<ChartOptions>(() => {
    this.i18n.lang();
    const rtl = this.i18n.isRtl();
    return {
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      plugins: {
        legend: { display: false },
        tooltip: this.aiTooltipOptions(rtl),
      },
    };
  });

  ngOnInit(): void {
    if (!this.canUseAi) return;
    this.lookupsApi.getSurveys('', 500).subscribe({
      next: (list) => this.surveyList.set(list ?? []),
      error: () => this.surveyList.set([]),
    });
  }

  runAnalyze(): void {
    if (!this.canUseAi) return;
    this.runWithBusy('analyze', () =>
      this.aiApi.analyzeReports({
        surveyId: this.selectedSurveyId || undefined,
        fromUtc: this.fromDate ? localDateStartUtc(this.fromDate) : undefined,
        toUtc: this.toDate ? localDateEndUtc(this.toDate) : undefined,
      }),
      (r) => {
        this.lastAnalyze.set(r);
        this.toast.show(this.i18n.t('q.aiScope.toast.analyzeOk'), 'success');
      }
    );
  }

  runSentiment(): void {
    if (!this.canUseAi) return;

    // If user didn't pick a survey nor dates, default to last 30 days (cross-survey sentiment).
    const filter: CrossSurveyAnalyticsFilterRequest = {
      surveyId: this.selectedSurveyId || undefined,
      fromUtc: this.fromDate ? localDateStartUtc(this.fromDate) : undefined,
      toUtc: this.toDate ? localDateEndUtc(this.toDate) : undefined,
    };

    this.runWithBusy(
      'sentiment',
      () =>
        this.aiApi.sentimentAnalysis({
          surveyId: this.selectedSurveyId || undefined,
          analyticsFilter: filter,
          defaultWindowDays: 30,
        }),
      (r) => {
        this.lastSentiment.set(r);
        this.toast.show(this.i18n.t('q.aiScope.toast.sentimentOk'), 'success');
      }
    );
  }

  runSuggestRecommendation(): void {
    if (!this.canUseAi) return;
    this.runWithBusy(
      'recommendation',
      () =>
        this.aiApi.suggestRecommendation({
          surveyId: this.selectedSurveyId || undefined,
        }),
      (r) => {
        this.lastRecommendation.set(r);
        this.toast.show(this.i18n.t('q.aiScope.toast.recoOk'), 'success');
      }
    );
  }

  runSuggestActionPlan(): void {
    if (!this.canUseAi) return;
    this.runWithBusy(
      'actionPlan',
      () =>
        this.aiApi.suggestActionPlan({
          surveyId: this.selectedSurveyId || undefined,
        }),
      (r) => {
        this.lastActionPlan.set(r);
        this.toast.show(this.i18n.t('q.aiScope.toast.apOk'), 'success');
      }
    );
  }

  clearResults(): void {
    this.lastAnalyze.set(null);
    this.lastSentiment.set(null);
    this.lastRecommendation.set(null);
    this.lastActionPlan.set(null);
    this.errorText.set('');
  }

  chartKind(c: AiChartSuggestionDto): 'bar' | 'line' | 'doughnut' {
    const k = (c.kind ?? 'bar').toLowerCase();
    return k === 'line' || k === 'doughnut' ? k : 'bar';
  }

  chartHeading(c: AiChartSuggestionDto): string {
    if (this.i18n.lang() === 'ar') return (c.titleAr || c.title || c.titleEn || '').trim();
    return (c.titleEn || c.title || c.titleAr || '').trim();
  }

  chartData(c: AiChartSuggestionDto): ChartData {
    const labels = c.labels ?? [];
    const values = (c.values ?? []).map((v) => Number(v));
    const palette = AiScopeActionsPageComponent.ChartPalette;
    const bg =
      this.chartKind(c) === 'doughnut'
        ? labels.map((_, i) => `${palette[i % palette.length]}bf`)
        : 'rgba(99, 102, 241, 0.55)';
    const borderCol = this.chartKind(c) === 'doughnut' ? labels.map((_, i) => palette[i % palette.length]) : '#6366f1';
    return {
      labels,
      datasets: [
        {
          data: values,
          label: this.chartHeading(c),
          backgroundColor: bg,
          borderColor: borderCol,
          borderWidth: this.chartKind(c) === 'doughnut' ? 2 : 1,
        },
      ],
    };
  }

  /** Bar/line beside sentiment doughnut — green / red / amber when labels are sentiment-like. */
  sentimentInsightChartData(c: AiChartSuggestionDto): ChartData<'bar' | 'line'> {
    const kind = this.chartKind(c) === 'line' ? 'line' : 'bar';
    return buildSentimentInsightChartData({
      chart: c,
      kind,
      datasetLabel: this.chartHeading(c),
    });
  }

  private aiTooltipOptions(rtl: boolean): TooltipOptions<any> {
    return {
      enabled: true,
      rtl,
      textDirection: rtl ? 'rtl' : 'ltr',
      position: 'nearest',
      intersect: false,
      backgroundColor: 'rgba(15, 23, 42, 0.94)',
      titleColor: '#f8fafc',
      bodyColor: '#e2e8f0',
      borderColor: 'rgba(148, 163, 184, 0.35)',
      borderWidth: 1,
      padding: 12,
      cornerRadius: 10,
      displayColors: true,
      boxPadding: 6,
      caretPadding: 10,
      caretSize: 7,
      titleFont: { size: 12, weight: 'bold' },
      bodyFont: { size: 12 },
    } as TooltipOptions<any>;
  }

  private runWithBusy<T>(kind: AiScopeRunKind, call: () => import('rxjs').Observable<T>, onOk: (value: T) => void): void {
    this.scopeRun.set(kind);
    this.errorText.set('');
    call().subscribe({
      next: (value) => {
        onOk(value);
        this.scopeRun.set('idle');
      },
      error: (err: unknown) => {
        this.scopeRun.set('idle');
        const msg =
          err instanceof ApiBusinessError && err.errors.length > 0
            ? err.errors[0]
            : this.i18n.t('q.aiScope.toast.err');
        this.errorText.set(msg);
        this.toast.show(msg, 'error');
      },
    });
  }
}

function localDateStartUtc(ymd: string): string {
  const d = new Date(`${ymd}T00:00:00`);
  return Number.isNaN(d.getTime()) ? new Date(ymd).toISOString() : d.toISOString();
}

function localDateEndUtc(ymd: string): string {
  const d = new Date(`${ymd}T23:59:59.999`);
  return Number.isNaN(d.getTime()) ? new Date(ymd).toISOString() : d.toISOString();
}

