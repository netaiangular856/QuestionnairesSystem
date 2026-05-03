import { Component, OnInit, OnDestroy, inject, signal, ElementRef, effect } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  SurveyComprehensiveAnalyticsDto,
  QuestionAnalyticsDto,
  AnswerDistributionDto,
  SurveyOverviewAnalytics,
} from '../../shared/models/questionnaire.models';
import { SurveysApiService } from '../../services/surveys-api.service';
import { I18nService } from '../../shared/services/i18n.service';
import { qLocalizedTitle } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { DecimalPipe } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { Chart, ChartData, ChartOptions, registerables } from 'chart.js';

/** Per-question chart: donut for parts-of-whole, horizontal bar for many categories */
interface QuestionChartSpec {
  chartType: 'doughnut' | 'bar';
  data: ChartData<'doughnut' | 'bar'>;
}

/** Runtime colors read from :host CSS variables (light + prefers-color-scheme: dark) */
interface ChartRuntimePalette {
  primary: string;
  primarySoft: string;
  primaryMid: string;
  sequence: string[];
  success: string;
  successInk: string;
  danger: string;
  dangerInk: string;
  sliceBorder: string;
  text: string;
  muted: string;
  grid: string;
  tooltip: string;
  tooltipTitle: string;
  tooltipBody: string;
  pointBorder: string;
}

@Component({
  selector: 'app-survey-question-analytics-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe, DecimalPipe, BaseChartDirective],
  templateUrl: './survey-question-analytics-page.component.html',
  styleUrl: './survey-question-analytics-page.component.scss',
})
export class SurveyQuestionAnalyticsPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly surveysApi = inject(SurveysApiService);
  private readonly hostRef = inject(ElementRef<HTMLElement>);
  private readonly i18n = inject(I18nService);

  /** Rebuild Chart.js datasets when locale or analytics payload changes */
  private readonly rebindChartsToLocale = effect(() => {
    this.i18n.lang();
    const data = this.analytics();
    if (!data) return;
    this.setupCharts(data);
  });

  surveyId = '';

  constructor() {
    Chart.register(...registerables);
  }

  readonly analytics = signal<SurveyComprehensiveAnalyticsDto | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(false);

  readonly timelineChartData = signal<ChartData<'line'>>({ datasets: [] });
  readonly categoryChartData = signal<ChartData<'doughnut'>>({ datasets: [] });
  readonly ratingChartData = signal<ChartData<'doughnut'>>({ datasets: [] });
  readonly questionChartData = signal<ChartData<'bar'>>({ datasets: [] });

  /** Questions that render a Chart.js widget (not KPI-only) */
  readonly questionChartSpecs = signal<Map<string, QuestionChartSpec>>(new Map());

  /** Chart.js options follow --qa-c-* variables (refreshed in setupCharts + on color-scheme change) */
  readonly lineChartOptions = signal<ChartOptions<'line'>>({ responsive: true, maintainAspectRatio: false });
  readonly radialChartOptions = signal<ChartOptions<'doughnut'>>({ responsive: true, maintainAspectRatio: false });
  readonly hBarChartOptions = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  /** Vertical columns — «أكثر الأسئلة استجابة» */
  readonly columnChartOptions = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });

  /** MCQ: use horizontal bar when more than this many options */
  private static readonly choiceBarThreshold = 6;

  private static readonly fontFamily = `system-ui, "Segoe UI", sans-serif`;

  private colorSchemeMql?: MediaQueryList;
  private readonly onColorSchemeChange = (): void => {
    const d = this.analytics();
    if (d) {
      this.setupCharts(d);
    }
  };

  ngOnInit(): void {
    this.surveyId = this.route.snapshot.paramMap.get('surveyId') ?? '';
    this.colorSchemeMql = globalThis.matchMedia?.('(prefers-color-scheme: dark)');
    this.colorSchemeMql?.addEventListener('change', this.onColorSchemeChange);
    this.load();
  }

  ngOnDestroy(): void {
    this.colorSchemeMql?.removeEventListener('change', this.onColorSchemeChange);
  }

  private readChartPaletteFromHost(): ChartRuntimePalette {
    const el = this.hostRef.nativeElement;
    const g = (name: string) => getComputedStyle(el).getPropertyValue(name).trim();
    const seq = [g('--qa-c-1'), g('--qa-c-2'), g('--qa-c-3'), g('--qa-c-4'), g('--qa-c-5'), g('--qa-c-6'), g('--qa-c-7'), g('--qa-c-8')].filter(
      Boolean,
    ) as string[];
    const fallbackSeq = ['#4338ca', '#4f46e5', '#6366f1', '#7c3aed', '#8b5cf6', '#06b6d4', '#0d9488', '#64748b'];
    return {
      primary: g('--qa-c-primary') || '#4f46e5',
      primarySoft: g('--qa-c-primary-soft') || 'rgba(79, 70, 229, 0.14)',
      primaryMid: g('--qa-c-primary-mid') || '#6366f1',
      sequence: seq.length ? seq : fallbackSeq,
      success: g('--qa-c-success') || '#10b981',
      successInk: g('--qa-c-success-ink') || '#059669',
      danger: g('--qa-c-danger') || '#f87171',
      dangerInk: g('--qa-c-danger-ink') || '#dc2626',
      sliceBorder: g('--qa-c-slice-border') || '#ffffff',
      text: g('--qa-c-text') || '#475569',
      muted: g('--qa-c-muted') || '#64748b',
      grid: g('--qa-c-grid') || 'rgba(100, 116, 139, 0.2)',
      tooltip: g('--qa-c-tooltip') || 'rgba(15, 23, 42, 0.91)',
      tooltipTitle: g('--qa-c-tooltip-title') || '#f8fafc',
      tooltipBody: g('--qa-c-tooltip-body') || '#e2e8f0',
      pointBorder: g('--qa-c-point-border') || '#ffffff',
    };
  }

  private buildLineOptions(p: ChartRuntimePalette): ChartOptions<'line'> {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: true,
          position: 'bottom',
          labels: {
            padding: 14,
            usePointStyle: true,
            boxWidth: 8,
            color: p.text,
            font: { size: 12, family: SurveyQuestionAnalyticsPageComponent.fontFamily },
          },
        },
        tooltip: {
          backgroundColor: p.tooltip,
          padding: 12,
          cornerRadius: 10,
          titleColor: p.tooltipTitle,
          bodyColor: p.tooltipBody,
          titleFont: { size: 13, weight: 600 },
          bodyFont: { size: 13 },
        },
      },
      scales: {
        x: {
          grid: { color: p.grid },
          ticks: { color: p.muted, font: { size: 11 } },
          border: { display: false },
        },
        y: {
          beginAtZero: true,
          grid: { color: p.grid },
          ticks: { color: p.muted, font: { size: 11 } },
          border: { display: false },
        },
      },
      elements: {
        line: { tension: 0.38, borderWidth: 2 },
        point: { radius: 4, hoverRadius: 7, borderWidth: 0 },
      },
    };
  }

  private buildRadialOptions(p: ChartRuntimePalette): ChartOptions<'doughnut'> {
    return {
      responsive: true,
      maintainAspectRatio: false,
      cutout: '58%',
      plugins: {
        legend: {
          position: 'bottom',
          labels: {
            padding: 12,
            usePointStyle: true,
            boxWidth: 8,
            color: p.text,
            font: { size: 11, family: SurveyQuestionAnalyticsPageComponent.fontFamily },
          },
        },
        tooltip: {
          backgroundColor: p.tooltip,
          padding: 12,
          cornerRadius: 10,
          titleColor: p.tooltipTitle,
          bodyColor: p.tooltipBody,
          titleFont: { size: 13, weight: 600 },
          bodyFont: { size: 13 },
        },
      },
    };
  }

  /** أعمدة رأسية — أعلى الأسئلة إجابة */
  private buildColumnOptions(p: ChartRuntimePalette): ChartOptions<'bar'> {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: {
          backgroundColor: p.tooltip,
          padding: 12,
          cornerRadius: 10,
          titleColor: p.tooltipTitle,
          bodyColor: p.tooltipBody,
          titleFont: { size: 13, weight: 600 },
          bodyFont: { size: 13 },
        },
      },
      scales: {
        x: {
          grid: { display: false },
          ticks: {
            color: p.text,
            font: { size: 10 },
            maxRotation: 50,
            minRotation: 0,
            autoSkip: true,
          },
          border: { display: false },
        },
        y: {
          beginAtZero: true,
          grid: { color: p.grid },
          ticks: { color: p.muted, font: { size: 11 } },
          border: { display: false },
        },
      },
      elements: {
        bar: {
          borderRadius: { topLeft: 8, topRight: 8, bottomLeft: 0, bottomRight: 0 },
          borderSkipped: false,
          borderWidth: 0,
        },
      },
    };
  }

  private buildHBarOptions(p: ChartRuntimePalette): ChartOptions<'bar'> {
    return {
      indexAxis: 'y',
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: {
          backgroundColor: p.tooltip,
          padding: 12,
          cornerRadius: 10,
          titleColor: p.tooltipTitle,
          bodyColor: p.tooltipBody,
          titleFont: { size: 13, weight: 600 },
          bodyFont: { size: 13 },
        },
      },
      scales: {
        x: {
          beginAtZero: true,
          grid: { color: p.grid },
          ticks: { color: p.muted, font: { size: 11 } },
          border: { display: false },
        },
        y: {
          grid: { display: false },
          ticks: { color: p.text, font: { size: 11 } },
          border: { display: false },
        },
      },
      elements: {
        bar: {
          borderRadius: 8,
          borderSkipped: false,
          borderWidth: 0,
        },
      },
    };
  }

  private refreshChartOptionSignals(): void {
    const p = this.readChartPaletteFromHost();
    this.lineChartOptions.set(this.buildLineOptions(p));
    this.radialChartOptions.set(this.buildRadialOptions(p));
    this.hBarChartOptions.set(this.buildHBarOptions(p));
    this.columnChartOptions.set(this.buildColumnOptions(p));
  }

  load(): void {
    if (!this.surveyId) return;
    this.busy.set(true);
    this.failed.set(false);
    this.surveysApi.getComprehensiveAnalytics(this.surveyId).subscribe({
      next: (data) => {
        this.busy.set(false);
        this.analytics.set(data);
      },
      error: () => {
        this.failed.set(true);
        this.busy.set(false);
      },
    });
  }

  showRatingSection(): boolean {
    const d = this.ratingChartData();
    const first = d.datasets?.[0];
    const len = first && 'data' in first ? (first.data as number[])?.length : 0;
    return !!len;
  }

  getQuestionSpec(questionId: string): QuestionChartSpec | undefined {
    return this.questionChartSpecs().get(questionId);
  }

  optionsForQuestionSpec(spec: QuestionChartSpec): ChartOptions {
    return (spec.chartType === 'bar' ? this.hBarChartOptions() : this.radialChartOptions()) as ChartOptions;
  }

  /** Share of submitted responses (0–100) for progress meter */
  answerSharePercent(question: QuestionAnalyticsDto, overview: SurveyOverviewAnalytics): number {
    if (!overview.submittedResponses || overview.submittedResponses <= 0) return 0;
    return Math.min(100, Math.round((question.totalAnswers / overview.submittedResponses) * 1000) / 10);
  }

  private setupCharts(data: SurveyComprehensiveAnalyticsDto): void {
    const P = this.readChartPaletteFromHost();

    const emptyTimelineLabel = this.i18n.t('q.detail.analytics.chartEmpty');
    const timelineLabels =
      data.responseTimeline.length > 0 ? data.responseTimeline.map((t) => t.date) : [emptyTimelineLabel];
    const timelineDataPoints =
      data.responseTimeline.length > 0 ? data.responseTimeline.map((t) => t.responseCount) : [0];

    this.timelineChartData.set({
      labels: timelineLabels,
      datasets: [
        {
          label: this.getChartLabel('responsesOverTime'),
          data: timelineDataPoints,
          borderColor: P.primary,
          backgroundColor: P.primarySoft,
          tension: 0.35,
          fill: true,
          borderWidth: 2,
          pointBackgroundColor: P.primaryMid,
          pointBorderColor: P.primaryMid,
          pointBorderWidth: 0,
          pointRadius: 5,
          pointHoverRadius: 7,
        },
      ],
    });

    const emptyCategoryLabel = this.i18n.t('q.detail.analytics.chartEmpty');
    const categoryLabels =
      data.categories.length > 0
        ? data.categories.map((c) => this.localizeCategoryLabel(c.categoryName))
        : [emptyCategoryLabel];
    const categoryDataPoints =
      data.categories.length > 0 ? data.categories.map((c) => c.responseCount) : [1];

    this.categoryChartData.set({
      labels: categoryLabels,
      datasets: [
        {
          data: categoryDataPoints,
          backgroundColor: P.sequence,
          borderWidth: 0,
          hoverOffset: 8,
        },
      ],
    });

    if (data.ratings.length > 0) {
      this.ratingChartData.set({
        labels: data.ratings.map((r) => String(r.rating)),
        datasets: [
          {
            data: data.ratings.map((r) => r.count),
            backgroundColor: data.ratings.map((_, i) => P.sequence[i % P.sequence.length]),
            borderWidth: 0,
            hoverOffset: 10,
          },
        ],
      });
    } else {
      this.ratingChartData.set({ datasets: [] });
    }

    const topQuestions =
      data.questions.length > 0
        ? data.questions.slice(0, 5)
        : [
            {
              questionId: 'no-questions',
              titleEn: emptyCategoryLabel,
              titleAr: emptyCategoryLabel,
              questionType: 'ShortText',
              totalAnswers: 0,
              answerDistribution: [],
              averageRating: undefined,
              minRating: undefined,
              maxRating: undefined,
            },
          ];

    this.questionChartData.set({
      labels: topQuestions.map((q) => {
        const title = this.getQuestionTitle(q);
        return title.length > 40 ? title.substring(0, 40) + '…' : title;
      }),
      datasets: [
        {
          label: this.getChartLabel('answersPerQuestion'),
          data: topQuestions.map((q) => q.totalAnswers),
          backgroundColor: topQuestions.map((_, i) => P.sequence[i % P.sequence.length]),
          borderWidth: 0,
          borderRadius: 8,
          maxBarThickness: 52,
        },
      ],
    });

    this.createQuestionCharts(data.questions);
    this.refreshChartOptionSignals();
  }

  private createQuestionCharts(questions: QuestionAnalyticsDto[]): void {
    const chartsMap = new Map<string, QuestionChartSpec>();
    const P = this.readChartPaletteFromHost();
    const barThreshold = SurveyQuestionAnalyticsPageComponent.choiceBarThreshold;

    for (const question of questions) {
      if (this.shouldUseKpiOnly(question)) {
        continue;
      }

      const dist = question.answerDistribution;
      if (!dist.length) {
        continue;
      }

      const labels = dist.map((ad: AnswerDistributionDto) => this.getDistributionLabel(question, ad));
      const values = dist.map((ad: AnswerDistributionDto) => ad.count);
      const n = dist.length;

      if (question.questionType === 'Rating' || question.questionType === 'Scale') {
        chartsMap.set(question.questionId, {
          chartType: 'doughnut',
          data: {
            labels,
            datasets: [
              {
                data: values,
                backgroundColor: labels.map((_, i) => P.sequence[i % P.sequence.length]),
                borderWidth: 0,
                hoverOffset: 8,
              },
            ],
          },
        });
        continue;
      }

      if (question.questionType === 'YesNo') {
        const bg = values.map((_, i) => (i % 2 === 0 ? P.success : P.danger));
        chartsMap.set(question.questionId, {
          chartType: 'doughnut',
          data: {
            labels,
            datasets: [{ data: values, backgroundColor: bg, borderWidth: 0, hoverOffset: 6 }],
          },
        });
        continue;
      }

      if (question.questionType === 'MultipleChoice' || question.questionType === 'SingleChoice') {
        if (n > barThreshold) {
          chartsMap.set(question.questionId, {
            chartType: 'bar',
            data: {
              labels,
              datasets: [
                {
                  label: this.getChartLabel('responses'),
                  data: values,
                  backgroundColor: labels.map((_, i) => P.sequence[i % P.sequence.length]),
                  borderWidth: 0,
                  borderRadius: 6,
                },
              ],
            },
          });
        } else {
          chartsMap.set(question.questionId, {
            chartType: 'doughnut',
            data: {
              labels,
              datasets: [
                {
                  data: values,
                  backgroundColor: labels.map((_, i) => P.sequence[i % P.sequence.length]),
                  borderWidth: 0,
                  hoverOffset: 8,
                },
              ],
            },
          });
        }
        continue;
      }

      if (question.questionType === 'Number' || question.questionType === 'Date') {
        chartsMap.set(question.questionId, {
          chartType: n > barThreshold ? 'bar' : 'doughnut',
          data:
            n > barThreshold
              ? {
                  labels,
                  datasets: [
                    {
                      label: this.getChartLabel('responses'),
                      data: values,
                      backgroundColor: labels.map((_, i) => P.sequence[i % P.sequence.length]),
                      borderWidth: 0,
                      borderRadius: 6,
                    },
                  ],
                }
              : {
                  labels,
                  datasets: [
                    {
                      data: values,
                      backgroundColor: labels.map((_, i) => P.sequence[i % P.sequence.length]),
                      borderWidth: 0,
                      hoverOffset: 8,
                    },
                  ],
                },
        });
      }
    }

    this.questionChartSpecs.set(chartsMap);
  }

  /**
   * Open-ended text (and similar): show KPI / progress only — not a distribution chart.
   * Also when the API sends no buckets for a question.
   */
  private shouldUseKpiOnly(question: QuestionAnalyticsDto): boolean {
    const t = question.questionType;
    const textLike = new Set(['ShortText', 'LongText', 'Email', 'Phone', 'Url']);
    if (textLike.has(t)) {
      return true;
    }
    if (!question.answerDistribution?.length) {
      return true;
    }
    return false;
  }

  getQuestionTitle(question: QuestionAnalyticsDto): string {
    return qLocalizedTitle(this.i18n.lang(), question.titleAr ?? '', question.titleEn ?? '');
  }

  getQuestionTypeLabel(questionType: string): string {
    const typeLabels: { [key: string]: { ar: string; en: string } } = {
      ShortText: { ar: 'نص قصير', en: 'Short text' },
      LongText: { ar: 'نص طويل', en: 'Long text' },
      SingleChoice: { ar: 'اختيار واحد', en: 'Single choice' },
      MultipleChoice: { ar: 'اختيار متعدد', en: 'Multiple choice' },
      Rating: { ar: 'تقييم', en: 'Rating' },
      Scale: { ar: 'مقياس', en: 'Scale' },
      YesNo: { ar: 'نعم/لا', en: 'Yes / No' },
      Number: { ar: 'رقم', en: 'Number' },
      Date: { ar: 'تاريخ', en: 'Date' },
      Email: { ar: 'بريد إلكتروني', en: 'Email' },
      Phone: { ar: 'هاتف', en: 'Phone' },
      Url: { ar: 'رابط', en: 'URL' },
    };

    const labels = typeLabels[questionType] || { ar: questionType, en: questionType };
    return this.i18n.lang() === 'ar' ? labels.ar : labels.en;
  }

  private localizeCategoryLabel(categoryName: string): string {
    return this.getQuestionTypeLabel(categoryName);
  }

  private getDistributionLabel(question: QuestionAnalyticsDto, ad: AnswerDistributionDto): string {
    const ar = (ad.optionTextAr ?? '').trim();
    const en = (ad.optionTextEn ?? '').trim();
    if (this.i18n.lang() === 'ar') {
      if (ar) return ar;
      if (en) return en;
    } else {
      if (en) return en;
      if (ar) return ar;
    }
    return (ad.optionText ?? '').trim();
  }

  private getChartLabel(key: string): string {
    const labels: { [key: string]: { ar: string; en: string } } = {
      responsesOverTime: { ar: 'الردود بمرور الوقت', en: 'Responses over time' },
      answersPerQuestion: { ar: 'إجابات لكل سؤال', en: 'Answers per question' },
      responses: { ar: 'ردود', en: 'Responses' },
      totalResponses: { ar: 'إجمالي الردود', en: 'All responses' },
      average: { ar: 'المتوسط', en: 'Average' },
      min: { ar: 'الحد الأدنى', en: 'Min' },
      max: { ar: 'الحد الأقصى', en: 'Max' },
    };

    const label = labels[key] || { ar: key, en: key };
    return this.i18n.lang() === 'ar' ? label.ar : label.en;
  }
}
