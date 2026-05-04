import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, effect, ElementRef, inject, signal, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { Chart, ChartData, ChartOptions, registerables } from 'chart.js';
import { AuthService } from '../../core/auth/auth.service';
import { ReportsApiService } from '../../services/reports-api.service';
import { DashboardFilterQuery, DashboardReportDto } from '../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { qLocalizedTitle } from '../../shared/questionnaires/q-display';

/** Matches `--rp-c-*` from reports dashboard styles (imported in component SCSS). */
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
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [TranslatePipe, RouterLink, DatePipe, DecimalPipe, BaseChartDirective, FormsModule],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
})
export class DashboardPageComponent implements OnInit {
  private readonly reports = inject(ReportsApiService);
  private readonly auth = inject(AuthService);
  private readonly i18n = inject(I18nService);
  private readonly hostRef = inject(ElementRef<HTMLElement>);

  private static readonly fontFamily = `system-ui, "Segoe UI", sans-serif`;

  readonly canReport = signal(this.auth.hasPermission(PermissionCodes.ReportView));
  readonly report = signal<DashboardReportDto | null>(null);
  readonly reportBusy = signal(false);
  readonly reportFailed = signal(false);
  readonly filterClientError = signal<string | null>(null);

  /** Bound with ngModel — yyyy-MM-dd interpreted as UTC calendar days. */
  filterDateFrom = '';
  filterDateTo = '';

  readonly lineData = signal<ChartData<'line'>>({ datasets: [] });
  /** Combined users / surveys / plans / initiatives created — single chart. */
  readonly lineActivityData = signal<ChartData<'line'>>({ datasets: [] });

  readonly doughnutSurveyData = signal<ChartData<'doughnut'>>({ datasets: [] });

  readonly barTopData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barDeptData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barAudienceData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barWeekdayData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barQuestionTypeData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barPartnerData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barRecommendationData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barInitiativeStatusData = signal<ChartData<'bar'>>({ datasets: [] });
  readonly barPlanStatusData = signal<ChartData<'bar'>>({ datasets: [] });

  readonly lineOpts = signal<ChartOptions<'line'>>({ responsive: true, maintainAspectRatio: false });
  readonly lineActivityOpts = signal<ChartOptions<'line'>>({ responsive: true, maintainAspectRatio: false });

  readonly doughnutSurveyOpts = signal<ChartOptions<'doughnut'>>({ responsive: true, maintainAspectRatio: false });

  readonly barTopOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barDeptOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barAudienceOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barWeekdayOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barQuestionOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barPartnerOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barRecommendationOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barInitiativeStatusOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barPlanStatusOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });

  private readonly syncCharts = effect(() => {
    this.i18n.lang();
    this.applyCharts(this.report());
  });

  constructor() {
    Chart.register(...registerables);
  }

  ngOnInit(): void {
    if (!this.canReport()) return;
    this.loadReport();
  }

  retry(): void {
    this.loadReport();
  }

  applyDateFilter(): void {
    this.filterClientError.set(null);
    const from = this.filterDateFrom?.trim();
    const to = this.filterDateTo?.trim();
    if (!from || !to) {
      this.filterClientError.set(this.i18n.t('dashboard.filter.bothRequired'));
      return;
    }
    if (from > to) {
      this.filterClientError.set(this.i18n.t('dashboard.filter.invalidOrder'));
      return;
    }
    const start = new Date(`${from}T00:00:00.000Z`);
    const endIncl = new Date(`${to}T00:00:00.000Z`);
    const spanDays = Math.floor((endIncl.getTime() - start.getTime()) / 86400000) + 1;
    if (spanDays > 366) {
      this.filterClientError.set(this.i18n.t('dashboard.filter.maxRange'));
      return;
    }
    this.loadReport();
  }

  clearDateFilter(): void {
    this.filterDateFrom = '';
    this.filterDateTo = '';
    this.filterClientError.set(null);
    this.loadReport();
  }

  /** Opens the native date picker when supported (calendar icon / picker UX). */
  openDatePicker(input: HTMLInputElement | null): void {
    if (!input || input.disabled) return;
    const anyIn = input as HTMLInputElement & { showPicker?: () => void };
    if (typeof anyIn.showPicker === 'function') {
      try {
        void anyIn.showPicker();
        return;
      } catch {
        /* ignore — fall back to focus */
      }
    }
    input.focus();
    input.click();
  }

  private loadReport(): void {
    this.reportBusy.set(true);
    this.reportFailed.set(false);
    const q = this.buildDashboardFilterQuery();
    this.reports.getDashboard(q).subscribe({
      next: (d) => {
        this.report.set(d);
        this.reportBusy.set(false);
      },
      error: () => {
        this.reportFailed.set(true);
        this.reportBusy.set(false);
      },
    });
  }

  private buildDashboardFilterQuery(): DashboardFilterQuery | undefined {
    const from = this.filterDateFrom?.trim();
    const to = this.filterDateTo?.trim();
    if (!from || !to) return undefined;
    if (from > to) return undefined;
    const fromUtc = `${from}T00:00:00.000Z`;
    const end = new Date(`${to}T00:00:00.000Z`);
    end.setUTCDate(end.getUTCDate() + 1);
    return { fromUtc, toUtc: end.toISOString() };
  }

  has(p: string): boolean {
    return this.auth.hasPermission(p);
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

  recommendationStatusLabel(key: string): string {
    const m: Record<string, string> = {
      Draft: 'q.recommendationStatus.draft',
      Active: 'q.recommendationStatus.active',
      Implemented: 'q.recommendationStatus.implemented',
      Dismissed: 'q.recommendationStatus.dismissed',
    };
    const tr = m[key];
    return tr ? this.i18n.t(tr) : key;
  }

  partnerTypeLabel(key: string): string {
    const m: Record<string, string> = {
      Dealer: 'q.partnerType.dealer',
      Partner: 'q.partnerType.partner',
      Vendor: 'q.partnerType.vendor',
      Customer: 'q.partnerType.customer',
      Other: 'q.partnerType.other',
    };
    const tr = m[key];
    return tr ? this.i18n.t(tr) : key;
  }

  questionTypeLabel(apiName: string): string {
    const k = `q.analysis.qtype.${apiName}`;
    const v = this.i18n.t(k);
    return v === k ? apiName : v;
  }

  weekdayShortLabel(key: string): string {
    const k = `q.analysis.dow.${key}`;
    const v = this.i18n.t(k);
    return v === k ? key : v;
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
      primarySoft: g('--rp-c-primary-soft') || 'rgba(79, 70, 229, 0.1)',
      sequence: seq.length ? seq : fallback,
      text: g('--rp-c-text') || '#475569',
      muted: g('--rp-c-muted') || '#64748b',
      grid: g('--rp-c-grid') || 'rgba(100, 116, 139, 0.2)',
      tooltipBg: g('--rp-c-tooltip') || 'rgba(15, 23, 42, 0.92)',
      tooltipTitle: g('--rp-c-tooltip-title') || '#f8fafc',
      tooltipBody: g('--rp-c-tooltip-body') || '#e2e8f0',
    };
  }

  private applyCharts(d: DashboardReportDto | null): void {
    const P = this.readPalette();
    const emptyLbl = this.i18n.t('q.detail.analytics.chartEmpty');

    if (!d) {
      this.resetEmptyCharts(P, emptyLbl);
      return;
    }

    const subs = d.submissionsTimelineLast30Days ?? [];
    this.lineData.set({
      labels: subs.length ? subs.map((x) => x.date) : [emptyLbl],
      datasets: [
        {
          label: this.i18n.t('dashboard.chart.submissions30d'),
          data: subs.length ? subs.map((x) => x.count) : [0],
          borderColor: P.primary,
          backgroundColor: P.primarySoft,
          fill: true,
          tension: 0.35,
          borderWidth: 2,
        },
      ],
    });

    const uReg = d.usersRegisteredTimelineLast30Days ?? [];
    const baseDates =
      subs.length > 0
        ? subs.map((x) => x.date)
        : uReg.length > 0
          ? uReg.map((x) => x.date)
          : (d.surveysCreatedTimelineLast30Days ?? []).map((x) => x.date);
    const labelsAct = baseDates.length ? baseDates : [emptyLbl];
    const alignCounts = (tl: { date: string; count: number }[]) =>
      labelsAct.map((date) => {
        if (!tl?.length) return 0;
        return tl.find((t) => t.date === date)?.count ?? 0;
      });

    const c1 = P.sequence[1] ?? P.primary;
    const c2 = P.sequence[2] ?? P.primary;
    const c3 = P.sequence[3] ?? P.primary;
    const c4 = P.sequence[4] ?? P.primary;
    this.lineActivityData.set({
      labels: labelsAct,
      datasets: [
        {
          label: this.i18n.t('dashboard.chart.usersRegistered30d'),
          data: alignCounts(d.usersRegisteredTimelineLast30Days ?? []),
          borderColor: c1,
          fill: false,
          tension: 0.35,
          borderWidth: 2,
          pointRadius: 0,
        },
        {
          label: this.i18n.t('dashboard.chart.surveysCreated30d'),
          data: alignCounts(d.surveysCreatedTimelineLast30Days ?? []),
          borderColor: c2,
          fill: false,
          tension: 0.35,
          borderWidth: 2,
          pointRadius: 0,
        },
        {
          label: this.i18n.t('dashboard.chart.plansCreated30d'),
          data: alignCounts(d.actionPlansCreatedTimelineLast30Days ?? []),
          borderColor: c3,
          fill: false,
          tension: 0.35,
          borderWidth: 2,
          pointRadius: 0,
        },
        {
          label: this.i18n.t('dashboard.chart.initiativesCreated30d'),
          data: alignCounts(d.initiativesCreatedTimelineLast30Days ?? []),
          borderColor: c4,
          fill: false,
          tension: 0.35,
          borderWidth: 2,
          pointRadius: 0,
        },
      ],
    });

    const setDoughnut = (
      dataRef: WritableSignal<ChartData<'doughnut'>>,
      rows: { key: string; count: number }[],
      labelFn: (k: string) => string,
      offset: number,
    ) => {
      dataRef.set({
        labels: rows.length ? rows.map((x) => labelFn(x.key)) : [emptyLbl],
        datasets: [
          {
            data: rows.length ? rows.map((x) => x.count) : [0],
            backgroundColor: rows.length
              ? rows.map((_, i) => P.sequence[(i + offset) % P.sequence.length])
              : [P.sequence[0]],
            borderWidth: 0,
          },
        ],
      });
    };

    setDoughnut(this.doughnutSurveyData, d.surveyStatusDistribution ?? [], (k) => this.surveyStatusLabel(k), 0);

    const iniSt = d.initiativeStatusDistribution ?? [];
    this.barInitiativeStatusData.set({
      labels: iniSt.length ? iniSt.map((x) => this.initiativeStatusLabel(x.key)) : [emptyLbl],
      datasets: [
        {
          label: this.i18n.t('dashboard.chart.initiativesByStatus'),
          data: iniSt.length ? iniSt.map((x) => x.count) : [0],
          backgroundColor: iniSt.length
            ? iniSt.map((_, i) => P.sequence[(i + 1) % P.sequence.length])
            : [P.sequence[0]],
          borderWidth: 0,
          borderRadius: 6,
        },
      ],
    });

    const planSt = d.actionPlanStatusDistribution ?? [];
    this.barPlanStatusData.set({
      labels: planSt.length ? planSt.map((x) => this.actionPlanStatusLabel(x.key)) : [emptyLbl],
      datasets: [
        {
          label: this.i18n.t('dashboard.chart.plansByStatus'),
          data: planSt.length ? planSt.map((x) => x.count) : [0],
          backgroundColor: planSt.length
            ? planSt.map((_, i) => P.sequence[(i + 3) % P.sequence.length])
            : [P.sequence[0]],
          borderWidth: 0,
          borderRadius: 6,
        },
      ],
    });

    const top = d.topSurveysBySubmissions ?? [];
    this.barTopData.set({
      labels: top.length
        ? top.map((r) => {
            const t = this.surveyTitle(r);
            return t.length > 42 ? `${t.slice(0, 40)}…` : t;
          })
        : [emptyLbl],
      datasets: [
        {
          label: this.i18n.t('dashboard.chart.topSurveys'),
          data: top.length ? top.map((r) => r.submissionsInPeriod) : [0],
          backgroundColor: top.length
            ? top.map((_, i) => P.sequence[i % P.sequence.length])
            : [P.sequence[0]],
          borderWidth: 0,
          borderRadius: 6,
        },
      ],
    });

    const depts = d.topDepartmentsByEmployees ?? [];
    this.barDeptData.set({
      labels: depts.length
        ? depts.map((r) => {
            const t = this.surveyTitle({ titleAr: r.titleAr, titleEn: r.titleEn });
            return t.length > 36 ? `${t.slice(0, 34)}…` : t;
          })
        : [emptyLbl],
      datasets: [
        {
          label: this.i18n.t('dashboard.chart.topDepartments'),
          data: depts.length ? depts.map((r) => r.employeeCount) : [0],
          backgroundColor: depts.length
            ? depts.map((_, i) => P.sequence[(i + 2) % P.sequence.length])
            : [P.sequence[0]],
          borderWidth: 0,
          borderRadius: 6,
        },
      ],
    });

    const aud = d.audienceScopeDistribution ?? [];
    this.barAudienceData.set({
      labels: aud.length ? aud.map((x) => this.audienceScopeLabel(x.key)) : [emptyLbl],
      datasets: [
        {
          label: this.i18n.t('q.analysis.chart.audienceScopeMix'),
          data: aud.length ? aud.map((x) => x.count) : [0],
          backgroundColor: aud.length
            ? aud.map((_, i) => P.sequence[i % P.sequence.length])
            : [P.sequence[0]],
          borderWidth: 0,
          borderRadius: { topLeft: 8, topRight: 8, bottomLeft: 0, bottomRight: 0 },
          maxBarThickness: 48,
        },
      ],
    });

    const dow = d.submissionsByDayOfWeek ?? [];
    this.barWeekdayData.set({
      labels: dow.length ? dow.map((x) => this.weekdayShortLabel(x.key)) : [emptyLbl],
      datasets: [
        {
          label: this.i18n.t('q.analysis.chart.submissionsByWeekday'),
          data: dow.length ? dow.map((x) => x.count) : [0],
          backgroundColor: dow.length
            ? dow.map((_, i) => P.sequence[i % P.sequence.length])
            : [P.sequence[0]],
          borderWidth: 0,
          borderRadius: 6,
        },
      ],
    });

    const qt = d.questionTypeDistribution ?? [];
    this.barQuestionTypeData.set({
      labels: qt.length ? qt.map((x) => this.questionTypeLabel(x.key)) : [emptyLbl],
      datasets: [
        {
          label: this.i18n.t('dashboard.chart.questionTypes'),
          data: qt.length ? qt.map((x) => x.count) : [0],
          backgroundColor: qt.length
            ? qt.map((_, i) => P.sequence[(i + 1) % P.sequence.length])
            : [P.sequence[0]],
          borderWidth: 0,
          borderRadius: 6,
        },
      ],
    });

    const pt = d.partnerTypeDistribution ?? [];
    this.barPartnerData.set({
      labels: pt.length ? pt.map((x) => this.partnerTypeLabel(x.key)) : [emptyLbl],
      datasets: [
        {
          label: this.i18n.t('dashboard.chart.partnerTypes'),
          data: pt.length ? pt.map((x) => x.count) : [0],
          backgroundColor: pt.length
            ? pt.map((_, i) => P.sequence[(i + 3) % P.sequence.length])
            : [P.sequence[0]],
          borderWidth: 0,
          borderRadius: 6,
        },
      ],
    });

    const rec = d.recommendationStatusDistribution ?? [];
    this.barRecommendationData.set({
      labels: rec.length ? rec.map((x) => this.recommendationStatusLabel(x.key)) : [emptyLbl],
      datasets: [
        {
          label: this.i18n.t('dashboard.chart.recommendationStatus'),
          data: rec.length ? rec.map((x) => x.count) : [0],
          backgroundColor: rec.length
            ? rec.map((_, i) => P.sequence[(i + 5) % P.sequence.length])
            : [P.sequence[0]],
          borderWidth: 0,
          borderRadius: 6,
        },
      ],
    });

    const dn = this.buildDoughnutOptions(P);
    this.doughnutSurveyOpts.set(dn);
    this.lineOpts.set(this.buildLineOptions(P));
    this.lineActivityOpts.set(this.buildLineActivityOptions(P));
    this.barTopOpts.set(this.buildBarOptions(P));
    this.barDeptOpts.set(this.buildBarOptions(P));
    this.barAudienceOpts.set(this.buildAudienceColumnBarOptions(P));
    this.barWeekdayOpts.set(this.buildWeekdayColumnBarOptions(P));
    this.barQuestionOpts.set(this.buildQuestionColumnBarOptions(P));
    this.barPartnerOpts.set(this.buildPartnerColumnBarOptions(P));
    this.barRecommendationOpts.set(this.buildRecommendationColumnBarOptions(P));
    this.barInitiativeStatusOpts.set(this.buildBarOptions(P));
    this.barPlanStatusOpts.set(this.buildBarOptions(P));
  }

  private resetEmptyCharts(P: RpChartPalette, emptyLbl: string): void {
    const emptyLine = (): ChartData<'line'> => ({
      labels: [emptyLbl],
      datasets: [
        {
          label: '',
          data: [0],
          borderColor: P.primary,
          backgroundColor: P.primarySoft,
          fill: true,
          tension: 0.35,
          borderWidth: 2,
        },
      ],
    });
    this.lineData.set(emptyLine());
    this.lineActivityData.set({
      labels: [emptyLbl],
      datasets: [
        { label: '', data: [0], borderColor: P.sequence[1], fill: false, borderWidth: 2 },
        { label: '', data: [0], borderColor: P.sequence[2], fill: false, borderWidth: 2 },
        { label: '', data: [0], borderColor: P.sequence[3], fill: false, borderWidth: 2 },
        { label: '', data: [0], borderColor: P.sequence[4], fill: false, borderWidth: 2 },
      ],
    });

    const z = (): ChartData<'doughnut'> => ({
      labels: [emptyLbl],
      datasets: [{ data: [0], backgroundColor: [P.sequence[0]], borderWidth: 0 }],
    });
    this.doughnutSurveyData.set(z());

    const zb = (): ChartData<'bar'> => ({
      labels: [emptyLbl],
      datasets: [{ label: '', data: [0], backgroundColor: P.sequence[0], borderWidth: 0, borderRadius: 6 }],
    });
    this.barTopData.set(zb());
    this.barDeptData.set(zb());
    this.barAudienceData.set(zb());
    this.barWeekdayData.set(zb());
    this.barQuestionTypeData.set(zb());
    this.barPartnerData.set(zb());
    this.barRecommendationData.set(zb());
    this.barInitiativeStatusData.set(zb());
    this.barPlanStatusData.set(zb());

    const dn = this.buildDoughnutOptions(P);
    this.doughnutSurveyOpts.set(dn);
    this.lineOpts.set(this.buildLineOptions(P));
    this.lineActivityOpts.set(this.buildLineActivityOptions(P));
    this.barTopOpts.set(this.buildBarOptions(P));
    this.barDeptOpts.set(this.buildBarOptions(P));
    this.barAudienceOpts.set(this.buildAudienceColumnBarOptions(P));
    this.barWeekdayOpts.set(this.buildWeekdayColumnBarOptions(P));
    this.barQuestionOpts.set(this.buildQuestionColumnBarOptions(P));
    this.barPartnerOpts.set(this.buildPartnerColumnBarOptions(P));
    this.barRecommendationOpts.set(this.buildRecommendationColumnBarOptions(P));
    this.barInitiativeStatusOpts.set(this.buildBarOptions(P));
    this.barPlanStatusOpts.set(this.buildBarOptions(P));
  }

  private buildLineActivityOptions(P: RpChartPalette): ChartOptions<'line'> {
    return {
      responsive: true,
      maintainAspectRatio: false,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: {
          display: true,
          position: 'bottom',
          labels: {
            color: P.text,
            font: { family: DashboardPageComponent.fontFamily, size: 11, weight: 600 },
            padding: 10,
            boxWidth: 10,
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
          ticks: { color: P.muted, maxRotation: 45, font: { size: 9, weight: 500 } },
          border: { display: false },
        },
        y: {
          beginAtZero: true,
          grid: { color: P.grid },
          ticks: { color: P.muted, font: { size: 11, weight: 500 } },
          border: { display: false },
        },
      },
    };
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
            font: { family: DashboardPageComponent.fontFamily, size: 13, weight: 600 },
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
          ticks: { color: P.muted, maxRotation: 45, minRotation: 0, font: { size: 10, weight: 500 } },
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
            font: { size: 11, family: DashboardPageComponent.fontFamily, weight: 600 },
            padding: 12,
            boxWidth: 11,
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
      layout: { padding: { top: 2, right: 4, bottom: 2, left: 2 } },
      scales: {
        x: {
          beginAtZero: true,
          grace: '0%',
          grid: { color: P.grid },
          ticks: { color: P.muted, font: { size: 12, weight: 500 } },
          border: { display: false },
        },
        y: {
          grid: { display: false },
          ticks: { color: P.text, font: { size: 11, weight: 600 } },
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

  private buildQuestionColumnBarOptions(P: RpChartPalette): ChartOptions<'bar'> {
    return this.buildWeekdayColumnBarOptions(P);
  }

  private buildPartnerColumnBarOptions(P: RpChartPalette): ChartOptions<'bar'> {
    return this.buildWeekdayColumnBarOptions(P);
  }

  private buildRecommendationColumnBarOptions(P: RpChartPalette): ChartOptions<'bar'> {
    return this.buildWeekdayColumnBarOptions(P);
  }
}
