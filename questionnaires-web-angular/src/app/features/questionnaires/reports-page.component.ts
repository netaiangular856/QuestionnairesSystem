import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Component, OnInit, effect, ElementRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { Chart, ChartData, ChartOptions, registerables } from 'chart.js';
import { AuthService } from '../../core/auth/auth.service';
import { QuestionnaireLookupsApiService } from '../../services/questionnaire-lookups-api.service';
import { ReportsApiService } from '../../services/reports-api.service';
import {
  CrossSurveyAnalyticsDto,
  CrossSurveyAnalyticsFilterRequest,
  LookupItemDto,
} from '../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { qLocalizedTitle } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { triggerBlobDownload } from '../../shared/utils/download-blob';

interface RpChartPalette {
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
  selector: 'app-reports-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe, DecimalPipe, DatePipe, FormsModule, BaseChartDirective],
  templateUrl: './reports-page.component.html',
  styleUrl: './reports-page.component.scss',
})
export class ReportsPageComponent implements OnInit {
  private readonly reportsApi = inject(ReportsApiService);
  private readonly lookupsApi = inject(QuestionnaireLookupsApiService);
  private readonly i18n = inject(I18nService);
  private readonly hostRef = inject(ElementRef<HTMLElement>);
  readonly auth = inject(AuthService);

  readonly canExport = this.auth.hasPermission(PermissionCodes.ReportExport);

  readonly data = signal<CrossSurveyAnalyticsDto | null>(null);
  readonly busy = signal(true);
  readonly failed = signal(false);
  readonly surveyList = signal<LookupItemDto[]>([]);

  readonly exportingPdf = signal(false);
  readonly exportingExcel = signal(false);
  readonly exportError = signal(false);

  fromDate = '';
  toDate = '';
  selectedSurveyId = '';

  readonly lineData = signal<ChartData<'line'>>({ datasets: [] });
  readonly doughnutSurveyData = signal<ChartData<'doughnut'>>({ datasets: [] });
  readonly doughnutResponseData = signal<ChartData<'doughnut'>>({ datasets: [] });
  readonly barTopData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barAudienceData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barWeekdayData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barRatingsData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly doughnutCategoriesData = signal<ChartData<'doughnut'>>({ datasets: [] });
  readonly barKeywordsData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly doughnutActionPlanData = signal<ChartData<'doughnut'>>({ datasets: [] });
  readonly doughnutInitiativeData = signal<ChartData<'doughnut'>>({ datasets: [] });

  readonly lineOpts = signal<ChartOptions<'line'>>({ responsive: true, maintainAspectRatio: false });
  readonly doughnutOpts = signal<ChartOptions<'doughnut'>>({ responsive: true, maintainAspectRatio: false });
  readonly doughnutResponseOpts = signal<ChartOptions<'doughnut'>>({ responsive: true, maintainAspectRatio: false });
  readonly barOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barAudienceOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barWeekdayOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barRatingsOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly doughnutCategoriesOpts = signal<ChartOptions<'doughnut'>>({ responsive: true, maintainAspectRatio: false });
  readonly barKeywordsOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly doughnutActionPlanOpts = signal<ChartOptions<'doughnut'>>({ responsive: true, maintainAspectRatio: false });
  readonly doughnutInitiativeOpts = signal<ChartOptions<'doughnut'>>({ responsive: true, maintainAspectRatio: false });

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
    const filter = this.buildAnalyticsFilter();
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

  private buildAnalyticsFilter(): CrossSurveyAnalyticsFilterRequest {
    return {
      surveyId: this.selectedSurveyId || undefined,
      fromUtc: this.fromDate ? new Date(`${this.fromDate}T00:00:00.000Z`).toISOString() : undefined,
      toUtc: this.toDate ? new Date(`${this.toDate}T23:59:59.999Z`).toISOString() : undefined,
    };
  }

  applyFilters(): void {
    this.load();
  }

  resetFilters(): void {
    this.fromDate = '';
    this.toDate = '';
    this.selectedSurveyId = '';
    this.exportError.set(false);
    this.load();
  }

  private buildExportFilter(): CrossSurveyAnalyticsFilterRequest {
    return {
      surveyId: this.selectedSurveyId || undefined,
      fromUtc: this.fromDate ? new Date(`${this.fromDate}T00:00:00.000Z`).toISOString() : undefined,
      toUtc: this.toDate ? new Date(`${this.toDate}T23:59:59.999Z`).toISOString() : undefined,
      lang: this.i18n.lang(),
    };
  }

  exportPdf(): void {
    if (!this.canExport || this.exportingPdf()) return;
    this.exportError.set(false);
    this.exportingPdf.set(true);
    const filter = this.buildExportFilter();
    const fallback = `survey-analytics-${new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-')}.pdf`;
    this.reportsApi.exportCrossSurveyAnalyticsPdf(filter).subscribe({
      next: (resp: HttpResponse<Blob>) => {
        this.exportingPdf.set(false);
        const ct = resp.headers.get('Content-Type') ?? '';
        if (ct.includes('application/json')) {
          this.exportError.set(true);
          return;
        }
        triggerBlobDownload(resp, fallback);
      },
      error: (err: unknown) => {
        this.exportingPdf.set(false);
        this.exportError.set(true);
        if (err instanceof HttpErrorResponse && err.error instanceof Blob) {
          err.error.text().then(() => undefined).catch(() => undefined);
        }
      },
    });
  }

  exportExcel(): void {
    if (!this.canExport || this.exportingExcel()) return;
    this.exportError.set(false);
    this.exportingExcel.set(true);
    const filter = this.buildExportFilter();
    const fallback = `survey-analytics-${new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-')}.xlsx`;
    this.reportsApi.exportCrossSurveyAnalyticsExcel(filter).subscribe({
      next: (resp: HttpResponse<Blob>) => {
        this.exportingExcel.set(false);
        const ct = resp.headers.get('Content-Type') ?? '';
        if (ct.includes('application/json')) {
          this.exportError.set(true);
          return;
        }
        triggerBlobDownload(resp, fallback);
      },
      error: () => {
        this.exportingExcel.set(false);
        this.exportError.set(true);
      },
    });
  }

  surveyTitle(row: { titleAr: string; titleEn: string }): string {
    return qLocalizedTitle(this.i18n.lang(), row.titleAr ?? '', row.titleEn ?? '');
  }

  linkedSurveyTitle(row: { linkedSurveyTitleAr?: string | null; linkedSurveyTitleEn?: string | null }): string {
    const ar = this.normalizeReportTitle(row.linkedSurveyTitleAr ?? '');
    const en = this.normalizeReportTitle(row.linkedSurveyTitleEn ?? '');
    const t = this.surveyTitle({ titleAr: ar, titleEn: en });
    return t.trim().length > 0 ? t : '—';
  }

  /**
   * Single readable title: dedupe doubled text in one field, collapse identical AR/EN,
   * then pick by UI language.
   */
  displayBilingualTitle(row: { titleAr?: string | null; titleEn?: string | null }): string {
    const ar = this.normalizeReportTitle(row.titleAr ?? '');
    const en = this.normalizeReportTitle(row.titleEn ?? '');
    if (!ar && !en) {
      return '—';
    }
    if (!ar) {
      return en;
    }
    if (!en) {
      return ar;
    }
    if (ar === en) {
      return ar;
    }
    return this.surveyTitle({ titleAr: ar, titleEn: en });
  }

  /** If the whole string is the same substring twice (e.g. pasted placeholder), return one half. */
  private normalizeReportTitle(raw: string): string {
    let t = raw.trim();
    if (t.length < 4) {
      return t;
    }
    if (t.length % 2 === 0) {
      const half = t.length / 2;
      const a = t.slice(0, half);
      const b = t.slice(half);
      if (a === b) {
        return a.trim();
      }
    }
    return t;
  }

  stripRowClass(prefix: 'ap' | 'in', status: string): string {
    const key = (status ?? '').replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
    return `rp-strip-item rp-strip-item--${prefix}-${key || 'unknown'}`;
  }

  actionPlanStripPercent(status: string): number {
    switch (status) {
      case 'Active':
        return 100;
      case 'Draft':
        return 42;
      case 'Completed':
        return 78;
      case 'Cancelled':
        return 24;
      default:
        return 55;
    }
  }

  initiativeStripPercent(status: string): number {
    switch (status) {
      case 'InProgress':
        return 100;
      case 'Planned':
        return 40;
      case 'Completed':
        return 80;
      case 'AtRisk':
        return 58;
      case 'Cancelled':
        return 22;
      default:
        return 50;
    }
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

  responseStatusLabel(key: string): string {
    const m: Record<string, string> = {
      InProgress: 'q.responseStatus.inProgress',
      Submitted: 'q.responseStatus.submitted',
      Invalid: 'q.responseStatus.invalid',
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

  questionTypeLabel(apiName: string): string {
    const key = `q.analysis.qtype.${apiName}`;
    const v = this.i18n.t(key);
    return v === key ? apiName : v;
  }

  private readPalette(): RpChartPalette {
    const el = this.hostRef.nativeElement;
    const g = (n: string) => getComputedStyle(el).getPropertyValue(n).trim();
    const seq = [
      g('--rp-c-1'),
      g('--rp-c-2'),
      g('--rp-c-3'),
      g('--rp-c-4'),
      g('--rp-c-5'),
      g('--rp-c-6'),
      g('--rp-c-7'),
    ].filter(Boolean) as string[];
    const fallback = ['#a5b4fc', '#818cf8', '#7dd3fc', '#7ee7d8', '#d8b4fe', '#fcd34d', '#cbd5e1'];
    return {
      primary: g('--rp-c-primary') || '#4f46e5',
      primarySoft: g('--rp-c-primary-soft') || 'rgba(79, 70, 229, 0.14)',
      sequence: seq.length ? seq : fallback,
      text: g('--rp-c-text') || '#475569',
      muted: g('--rp-c-muted') || '#64748b',
      grid: g('--rp-c-grid') || 'rgba(100, 116, 139, 0.2)',
      tooltipBg: g('--rp-c-tooltip') || 'rgba(15, 23, 42, 0.92)',
      tooltipTitle: g('--rp-c-tooltip-title') || '#f8fafc',
      tooltipBody: g('--rp-c-tooltip-body') || '#e2e8f0',
    };
  }

  private applyCharts(d: CrossSurveyAnalyticsDto): void {
    const P = this.readPalette();
    const emptyLbl = this.i18n.t('q.detail.analytics.chartEmpty');

    const tl = d.submissionsByDay;
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

    const rs = d.responseStatusDistribution ?? [];
    this.doughnutResponseData.set({
      labels: rs.length ? rs.map((x) => this.responseStatusLabel(x.key)) : [emptyLbl],
      datasets: [
        {
          data: rs.length ? rs.map((x) => x.count) : [0],
          backgroundColor: rs.length
            ? rs.map((_, i) => P.sequence[(i + 3) % P.sequence.length])
            : [P.sequence[0]],
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
    this.doughnutResponseOpts.set(this.buildDoughnutOptions(P));
    this.barOpts.set(this.buildBarOptions(P));
    this.barAudienceOpts.set(this.buildAudienceColumnBarOptions(P));
    this.barWeekdayOpts.set(this.buildWeekdayColumnBarOptions(P));

    const apStatus = d.actionPlanStatusDistribution ?? [];
    this.doughnutActionPlanData.set({
      labels: apStatus.length ? apStatus.map((x) => this.actionPlanStatusLabel(x.key)) : [emptyLbl],
      datasets: [
        {
          data: apStatus.length ? apStatus.map((x) => x.count) : [0],
          backgroundColor: apStatus.length
            ? apStatus.map((_, i) => P.sequence[(i + 2) % P.sequence.length])
            : [P.sequence[0]],
          borderWidth: 0,
        },
      ],
    });

    const iniStatus = d.initiativeStatusDistribution ?? [];
    this.doughnutInitiativeData.set({
      labels: iniStatus.length ? iniStatus.map((x) => this.initiativeStatusLabel(x.key)) : [emptyLbl],
      datasets: [
        {
          data: iniStatus.length ? iniStatus.map((x) => x.count) : [0],
          backgroundColor: iniStatus.length
            ? iniStatus.map((_, i) => P.sequence[(i + 4) % P.sequence.length])
            : [P.sequence[1]],
          borderWidth: 0,
        },
      ],
    });

    this.doughnutActionPlanOpts.set(this.buildDoughnutOptions(P));
    this.doughnutInitiativeOpts.set(this.buildDoughnutOptions(P));
  }

  actionPlanStatusLabel(key: string): string {
    const m: Record<string, string> = {
      Draft: 'q.actionPlanStatus.draft',
      Active: 'q.actionPlanStatus.active',
      Completed: 'q.actionPlanStatus.completed',
      Cancelled: 'q.actionPlanStatus.cancelled',
    };
    const tr = m[key];
    return tr ? this.i18n.t(tr) : key;
  }

  initiativeStatusLabel(key: string): string {
    const m: Record<string, string> = {
      Planned: 'q.initiativeStatus.planned',
      InProgress: 'q.initiativeStatus.inProgress',
      Completed: 'q.initiativeStatus.completed',
      AtRisk: 'q.initiativeStatus.atRisk',
      Cancelled: 'q.initiativeStatus.cancelled',
    };
    const tr = m[key];
    return tr ? this.i18n.t(tr) : key;
  }

  private applyInsightCharts(d: CrossSurveyAnalyticsDto | null): void {
    const P = this.readPalette();
    const emptyLbl = this.i18n.t('q.detail.analytics.chartEmpty');

    if (!d) {
      this.barRatingsData.set({
        labels: [emptyLbl],
        datasets: [{ data: [0], label: '', backgroundColor: P.sequence[0], borderWidth: 0 }],
      });
      this.doughnutCategoriesData.set({
        labels: [emptyLbl],
        datasets: [{ data: [0], backgroundColor: [P.sequence[0]], borderWidth: 0 }],
      });
      this.barKeywordsData.set({
        labels: [emptyLbl],
        datasets: [{ data: [0], label: '', backgroundColor: P.sequence[0], borderWidth: 0 }],
      });
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

  private buildLineOptions(P: RpChartPalette): ChartOptions<'line'> {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: true,
          position: 'bottom',
          labels: {
            color: P.text,
            font: { family: ReportsPageComponent.fontFamily, size: 13, weight: 600 },
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

  private buildDoughnutOptions(P: RpChartPalette): ChartOptions<'doughnut'> {
    return {
      responsive: true,
      maintainAspectRatio: false,
      cutout: '56%',
      plugins: {
        legend: {
          position: 'bottom',
          labels: {
            color: P.text,
            font: { size: 12, family: ReportsPageComponent.fontFamily, weight: 600 },
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

  private buildBarOptions(P: RpChartPalette): ChartOptions<'bar'> {
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

  private buildAudienceColumnBarOptions(P: RpChartPalette): ChartOptions<'bar'> {
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

  private buildRatingsColumnBarOptions(P: RpChartPalette): ChartOptions<'bar'> {
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

  private buildWeekdayColumnBarOptions(P: RpChartPalette): ChartOptions<'bar'> {
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
