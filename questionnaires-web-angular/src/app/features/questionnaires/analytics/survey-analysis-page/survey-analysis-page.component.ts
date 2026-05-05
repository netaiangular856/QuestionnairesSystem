import { DecimalPipe } from '@angular/common';
import { Component, OnInit, effect, ElementRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { Chart, ChartData, ChartOptions, registerables } from 'chart.js';
import { QuestionnaireLookupsApiService } from '../../../../services/questionnaire-lookups-api.service';
import { ReportsApiService } from '../../../../services/reports-api.service';
import {
  CrossSurveyAnalyticsDto,
  CrossSurveyAnalyticsFilterRequest,
  LookupItemDto,
} from '../../../../shared/models/questionnaire.models';
import { qLocalizedTitle } from '../../../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';

interface SaChartPalette {
  primary: string;
  primarySoft: string;
  sequence: string[];
  text: string;
  muted: string;
  grid: string;
  tooltipBg: string;
  tooltipTitle: string;
  tooltipBody: string;
}

@Component({
  selector: 'app-survey-analysis-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe, DecimalPipe, FormsModule, BaseChartDirective],
  templateUrl: './survey-analysis-page.component.html',
  styleUrl: './survey-analysis-page.component.scss',
})
export class SurveyAnalysisPageComponent implements OnInit {
  private readonly reportsApi = inject(ReportsApiService);
  private readonly lookupsApi = inject(QuestionnaireLookupsApiService);
  private readonly i18n = inject(I18nService);
  private readonly hostRef = inject(ElementRef<HTMLElement>);

  readonly data = signal<CrossSurveyAnalyticsDto | null>(null);
  readonly busy = signal(false);
  readonly failed = signal(false);
  readonly surveyList = signal<LookupItemDto[]>([]);

  fromDate = '';
  toDate = '';
  selectedSurveyId = '';

  readonly lineData = signal<ChartData<'line'>>({ datasets: [] });
  readonly doughnutSurveyData = signal<ChartData<'doughnut'>>({ datasets: [] });
  readonly barTopData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barAudienceData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barWeekdayData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barRatingsData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly doughnutCategoriesData = signal<ChartData<'doughnut'>>({ datasets: [] });
  readonly barKeywordsData = signal<ChartData<'bar'>>({ datasets: [] });

  readonly lineOpts = signal<ChartOptions<'line'>>({ responsive: true, maintainAspectRatio: false });
  readonly doughnutOpts = signal<ChartOptions<'doughnut'>>({ responsive: true, maintainAspectRatio: false });
  readonly barOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barAudienceOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barWeekdayOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barRatingsOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly doughnutCategoriesOpts = signal<ChartOptions<'doughnut'>>({ responsive: true, maintainAspectRatio: false });
  readonly barKeywordsOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });

  private static readonly fontFamily = `system-ui, "Segoe UI", sans-serif`;

  private readonly localeCharts = effect(() => {
    this.i18n.lang();
    const d = this.data();
    if (d) {
      this.applyCharts(d);
      this.applyInsightCharts(d);
    } else {
      this.applyInsightCharts(null);
    }
  });

  constructor() {
    Chart.register(...registerables);
  }

  ngOnInit(): void {
    this.lookupsApi.getSurveys('', 500).subscribe({
      next: (list) => this.surveyList.set([...list]),
      error: () => this.surveyList.set([]),
    });
    this.load();
  }

  load(): void {
    this.busy.set(true);
    this.failed.set(false);
    const filter = this.buildFilter();
    this.reportsApi.getCrossSurveyAnalytics(filter).subscribe({
      next: (d) => {
        this.data.set(d);
        this.busy.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.busy.set(false);
      },
    });
  }

  private buildFilter(): CrossSurveyAnalyticsFilterRequest {
    return {
      surveyId: this.selectedSurveyId || undefined,
      fromUtc: this.fromDate ? new Date(`${this.fromDate}T00:00:00.000Z`).toISOString() : undefined,
      toUtc: this.toDate ? new Date(`${this.toDate}T23:59:59.999Z`).toISOString() : undefined,
    };
  }

  apply(): void {
    this.load();
  }

  reset(): void {
    this.fromDate = '';
    this.toDate = '';
    this.selectedSurveyId = '';
    this.load();
  }

  surveyTitle(row: { titleAr: string; titleEn: string }): string {
    return qLocalizedTitle(this.i18n.lang(), row.titleAr ?? '', row.titleEn ?? '');
  }

  surveyStatusLabel(key: string): string {
    const m: Record<string, string> = {
      Draft: 'q.surveyStatus.draft',
      PendingApproval: 'q.surveyStatus.pending',
      Approved: 'q.surveyStatus.approved',
      Published: 'q.surveyStatus.published',
      Closed: 'q.surveyStatus.closed',
      Rejected: 'q.surveyStatus.rejected',
    };
    const tr = m[key];
    return tr ? this.i18n.t(tr) : key;
  }

  audienceScopeLabel(key: string): string {
    const m: Record<string, string> = {
      Everyone: 'q.audience.everyone',
      Guest: 'q.audience.guest',
      SpecificUsers: 'q.audience.specific',
      AllOrganizationMembers: 'q.audience.org',
    };
    const tr = m[key];
    return tr ? this.i18n.t(tr) : key;
  }

  weekdayShortLabel(key: string): string {
    const k = `q.analysis.dow.${key}`;
    const v = this.i18n.t(k);
    return v === k ? key : v;
  }

  private readPalette(): SaChartPalette {
    const el = this.hostRef.nativeElement;
    const g = (n: string) => getComputedStyle(el).getPropertyValue(n).trim();
    const seq = [
      g('--sa-c-1'),
      g('--sa-c-2'),
      g('--sa-c-3'),
      g('--sa-c-4'),
      g('--sa-c-5'),
      g('--sa-c-6'),
      g('--sa-c-7'),
    ].filter(Boolean) as string[];
    const fallback = ['#a5b4fc', '#818cf8', '#7dd3fc', '#7ee7d8', '#d8b4fe', '#fcd34d', '#cbd5e1'];
    return {
      primary: g('--sa-c-primary') || '#4f46e5',
      primarySoft: g('--sa-c-primary-soft') || 'rgba(79, 70, 229, 0.14)',
      sequence: seq.length ? seq : fallback,
      text: g('--sa-c-text') || '#475569',
      muted: g('--sa-c-muted') || '#64748b',
      grid: g('--sa-c-grid') || 'rgba(100, 116, 139, 0.2)',
      tooltipBg: g('--sa-c-tooltip') || 'rgba(15, 23, 42, 0.92)',
      tooltipTitle: g('--sa-c-tooltip-title') || '#f8fafc',
      tooltipBody: g('--sa-c-tooltip-body') || '#e2e8f0',
    };
  }

  private applyCharts(d: CrossSurveyAnalyticsDto): void {
    const P = this.readPalette();

    const tl = d.submissionsByDay;
    const emptyLbl = this.i18n.t('q.detail.analytics.chartEmpty');
    this.lineData.set({
      labels: tl.length ? tl.map((x) => x.date) : [emptyLbl],
      datasets: [
        {
          label: this.i18n.t('q.analysis.chart.submissionsOverTime'),
          data: tl.length ? tl.map((x) => x.count) : [0],
          borderColor: P.primary,
          backgroundColor: P.primarySoft,
          fill: true,
          tension: 0.35,
          borderWidth: 2,
        },
      ],
    });

    const ss = d.surveyStatusDistribution;
    this.doughnutSurveyData.set({
      labels: ss.map((x) => this.surveyStatusLabel(x.key)),
      datasets: [
        {
          data: ss.map((x) => x.count),
          backgroundColor: ss.map((_, i) => P.sequence[i % P.sequence.length]),
          borderWidth: 0,
        },
      ],
    });

    const top = d.topSurveysBySubmissions;
    this.barTopData.set({
      labels: top.map((r) => {
        const t = this.surveyTitle(r);
        return t.length > 48 ? `${t.slice(0, 46)}…` : t;
      }),
      datasets: [
        {
          label: this.i18n.t('q.analysis.chart.topSurveys'),
          data: top.map((r) => r.submissionsInPeriod),
          backgroundColor: top.map((_, i) => P.sequence[i % P.sequence.length]),
          borderWidth: 0,
          borderRadius: 6,
        },
      ],
    });

    const aud = d.audienceScopeDistribution ?? [];
    this.barAudienceData.set({
      labels: aud.map((x) => this.audienceScopeLabel(x.key)),
      datasets: [
        {
          label: this.i18n.t('q.analysis.chart.audienceScopeMix'),
          data: aud.map((x) => x.count),
          backgroundColor: aud.map((_, i) => P.sequence[(i + 1) % P.sequence.length]),
          borderWidth: 0,
          borderRadius: { topLeft: 8, topRight: 8, bottomLeft: 0, bottomRight: 0 },
          maxBarThickness: 48,
        },
      ],
    });

    const dow = d.submissionsByDayOfWeek ?? [];
    this.barWeekdayData.set({
      labels: dow.map((x) => this.weekdayShortLabel(x.key)),
      datasets: [
        {
          label: this.i18n.t('q.analysis.chart.submissionsByWeekday'),
          data: dow.map((x) => x.count),
          backgroundColor: dow.map((_, i) => P.sequence[i % P.sequence.length]),
          borderWidth: 0,
          borderRadius: 6,
        },
      ],
    });

    this.lineOpts.set(this.buildLineOptions(P));
    this.doughnutOpts.set(this.buildDoughnutOptions(P));
    this.barOpts.set(this.buildBarOptions(P));
    this.barAudienceOpts.set(this.buildAudienceColumnBarOptions(P));
    this.barWeekdayOpts.set(this.buildWeekdayColumnBarOptions(P));
  }

  private applyInsightCharts(d: CrossSurveyAnalyticsDto | null): void {
    const P = this.readPalette();
    const emptyLbl = this.i18n.t('q.detail.analytics.chartEmpty');

    if (!d) {
      this.barRatingsData.set({ labels: [emptyLbl], datasets: [{ data: [0], label: '', backgroundColor: P.sequence[0], borderWidth: 0 }] });
      this.doughnutCategoriesData.set({ labels: [emptyLbl], datasets: [{ data: [0], backgroundColor: [P.sequence[0]], borderWidth: 0 }] });
      this.barKeywordsData.set({ labels: [emptyLbl], datasets: [{ data: [0], label: '', backgroundColor: P.sequence[0], borderWidth: 0 }] });
      this.barRatingsOpts.set(this.buildRatingsColumnBarOptions(P));
      this.doughnutCategoriesOpts.set(this.buildDoughnutOptions(P));
      this.barKeywordsOpts.set(this.buildBarOptions(P));
      return;
    }

    const ratings = d.ratingsDistribution ?? [];
    this.barRatingsData.set({
      labels: ratings.length ? ratings.map((r) => String(r.rating)) : [emptyLbl],
      datasets: [
        {
          label: this.i18n.t('q.analysis.chart.ratingBars'),
          data: ratings.length ? ratings.map((r) => r.count) : [0],
          backgroundColor: ratings.map((_, i) => P.sequence[i % P.sequence.length]),
          borderWidth: 0,
          borderRadius: 6,
          maxBarThickness: 56,
        },
      ],
    });

    const qtypes = d.questionTypeAnswerTotals ?? [];
    this.doughnutCategoriesData.set({
      labels: qtypes.length ? qtypes.map((c) => this.questionTypeLabel(c.key)) : [emptyLbl],
      datasets: [
        {
          data: qtypes.length ? qtypes.map((c) => c.count) : [0],
          backgroundColor: qtypes.map((_, i) => P.sequence[(i + 2) % P.sequence.length]),
          borderWidth: 0,
        },
      ],
    });

    const kws = d.textAnswerKeywords ?? [];
    this.barKeywordsData.set({
      labels: kws.length
        ? kws.map((k) => {
            const w = k.keyword ?? '';
            return w.length > 36 ? `${w.slice(0, 34)}…` : w;
          })
        : [emptyLbl],
      datasets: [
        {
          label: this.i18n.t('q.analysis.chart.textKeywords'),
          data: kws.length ? kws.map((k) => k.count) : [0],
          backgroundColor: kws.map((_, i) => P.sequence[(i + 1) % P.sequence.length]),
          borderWidth: 0,
          borderRadius: 6,
        },
      ],
    });

    this.barRatingsOpts.set(this.buildRatingsColumnBarOptions(P));
    this.doughnutCategoriesOpts.set(this.buildDoughnutOptions(P));
    this.barKeywordsOpts.set(this.buildBarOptions(P));
  }

  questionTypeLabel(apiName: string): string {
    const key = `q.analysis.qtype.${apiName}`;
    const v = this.i18n.t(key);
    return v === key ? apiName : v;
  }

  private buildLineOptions(P: SaChartPalette): ChartOptions<'line'> {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: true,
          position: 'bottom',
          labels: {
            color: P.text,
            font: { family: SurveyAnalysisPageComponent.fontFamily, size: 13, weight: 600 },
            padding: 14,
          },
        },
        tooltip: {
          backgroundColor: P.tooltipBg,
          titleColor: P.tooltipTitle,
          bodyColor: P.tooltipBody,
          padding: 12,
          cornerRadius: 10,
        },
      },
      scales: {
        x: {
          grid: { color: P.grid },
          ticks: { color: P.muted, font: { size: 12, weight: 500 } },
          border: { display: false },
        },
        y: {
          beginAtZero: true,
          grid: { color: P.grid },
          ticks: { color: P.muted, font: { size: 12, weight: 500 } },
          border: { display: false },
        },
      },
    };
  }

  private buildDoughnutOptions(P: SaChartPalette): ChartOptions<'doughnut'> {
    return {
      responsive: true,
      maintainAspectRatio: false,
      cutout: '56%',
      plugins: {
        legend: {
          position: 'bottom',
          labels: {
            color: P.text,
            font: { size: 12, family: SurveyAnalysisPageComponent.fontFamily, weight: 600 },
            padding: 14,
          },
        },
        tooltip: {
          backgroundColor: P.tooltipBg,
          titleColor: P.tooltipTitle,
          bodyColor: P.tooltipBody,
          padding: 12,
          cornerRadius: 10,
        },
      },
    };
  }

  private buildBarOptions(P: SaChartPalette): ChartOptions<'bar'> {
    return {
      indexAxis: 'y',
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: {
          backgroundColor: P.tooltipBg,
          titleColor: P.tooltipTitle,
          bodyColor: P.tooltipBody,
          padding: 12,
          cornerRadius: 10,
        },
      },
      scales: {
        x: {
          beginAtZero: true,
          grid: { color: P.grid },
          ticks: { color: P.muted, font: { size: 12, weight: 500 } },
          border: { display: false },
        },
        y: {
          grid: { display: false },
          ticks: { color: P.text, font: { size: 12, weight: 600 } },
          border: { display: false },
        },
      },
    };
  }

  /** Vertical columns — audience scope. */
  private buildAudienceColumnBarOptions(P: SaChartPalette): ChartOptions<'bar'> {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: false,
        },
        tooltip: {
          backgroundColor: P.tooltipBg,
          titleColor: P.tooltipTitle,
          bodyColor: P.tooltipBody,
          padding: 12,
          cornerRadius: 10,
        },
      },
      scales: {
        x: {
          grid: { display: false },
          ticks: { color: P.text, font: { size: 11, weight: 600 }, maxRotation: 35 },
          border: { display: false },
        },
        y: {
          beginAtZero: true,
          grid: { color: P.grid },
          ticks: { color: P.muted, font: { size: 12, weight: 500 } },
          border: { display: false },
        },
      },
    };
  }

  /** Vertical columns — rating counts. */
  private buildRatingsColumnBarOptions(P: SaChartPalette): ChartOptions<'bar'> {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: {
          backgroundColor: P.tooltipBg,
          titleColor: P.tooltipTitle,
          bodyColor: P.tooltipBody,
          padding: 12,
          cornerRadius: 10,
        },
      },
      scales: {
        x: {
          grid: { display: false },
          ticks: { color: P.text, font: { size: 11, weight: 600 } },
          border: { display: false },
        },
        y: {
          beginAtZero: true,
          grid: { color: P.grid },
          ticks: { color: P.muted, font: { size: 12, weight: 500 } },
          border: { display: false },
        },
      },
    };
  }

  /** Vertical bars — weekdays. */
  private buildWeekdayColumnBarOptions(P: SaChartPalette): ChartOptions<'bar'> {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: {
          backgroundColor: P.tooltipBg,
          titleColor: P.tooltipTitle,
          bodyColor: P.tooltipBody,
          padding: 12,
          cornerRadius: 10,
        },
      },
      scales: {
        x: {
          grid: { display: false },
          ticks: { color: P.text, font: { size: 11, weight: 600 }, maxRotation: 45 },
          border: { display: false },
        },
        y: {
          beginAtZero: true,
          grid: { color: P.grid },
          ticks: { color: P.muted, font: { size: 12, weight: 500 } },
          border: { display: false },
        },
      },
    };
  }
}
