import { Component, OnInit, effect, ElementRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective } from 'ng2-charts';
import { Chart, ChartData, ChartOptions, registerables } from 'chart.js';
import { ActionPlansApiService } from '../../../../services/action-plans-api.service';
import { InitiativesApiService } from '../../../../services/initiatives-api.service';
import { QuestionnaireLookupsApiService } from '../../../../services/questionnaire-lookups-api.service';
import { ReportsApiService } from '../../../../services/reports-api.service';
import {
  ActionPlanDto,
  ImpactMeasurementDto,
  ImpactMeasurementFilterRequest,
  ImpactMeasurementOverviewDto,
  InitiativeListItemDto,
  LookupItemDto,
  NamedCountDto,
} from '../../../../shared/models/questionnaire.models';
import { qLocalizedTitle } from '../../../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';
import { ApiBusinessError } from '../../../../shared/utils/api-helpers';

type SliceKey = 'survey' | 'exec';

@Component({
  selector: 'app-impact-measurement-page',
  standalone: true,
  imports: [TranslatePipe, FormsModule, BaseChartDirective],
  templateUrl: './impact-measurement-page.component.html',
  styleUrl: './impact-measurement-page.component.scss',
})
export class ImpactMeasurementPageComponent implements OnInit {
  private readonly reportsApi = inject(ReportsApiService);
  private readonly lookupsApi = inject(QuestionnaireLookupsApiService);
  private readonly actionPlansApi = inject(ActionPlansApiService);
  private readonly initiativesApi = inject(InitiativesApiService);
  readonly i18n = inject(I18nService);
  private readonly hostRef = inject(ElementRef<HTMLElement>);

  readonly data = signal<ImpactMeasurementOverviewDto | null>(null);
  readonly busy = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly surveyList = signal<LookupItemDto[]>([]);
  readonly planList = signal<ActionPlanDto[]>([]);
  readonly initiativeList = signal<InitiativeListItemDto[]>([]);

  selectedSurveyId = '';
  selectedPlanId = '';
  selectedInitiativeId = '';

  fromDate = '';
  toDate = '';
  splitAtLocal = '';

  readonly surveyBarVolumeData = signal<ChartData<'bar'>>({ labels: [], datasets: [] });
  readonly surveyBarAvgData = signal<ChartData<'bar'>>({ labels: [], datasets: [] });
  readonly surveyBarAvgHorizontalData = signal<ChartData<'bar'>>({ labels: [], datasets: [] });
  readonly surveyLineTrendData = signal<ChartData<'line'>>({ labels: [], datasets: [] });

  readonly execBarVolumeData = signal<ChartData<'bar'>>({ labels: [], datasets: [] });
  readonly execBarAvgData = signal<ChartData<'bar'>>({ labels: [], datasets: [] });
  readonly execBarAvgHorizontalData = signal<ChartData<'bar'>>({ labels: [], datasets: [] });
  readonly execLineTrendData = signal<ChartData<'line'>>({ labels: [], datasets: [] });
  readonly execDoughnutStatusData = signal<ChartData<'doughnut'>>({ labels: [], datasets: [] });
  readonly execPolarStatusData = signal<ChartData<'polarArea'>>({ labels: [], datasets: [] });

  readonly barOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly barHorizontalOpts = signal<ChartOptions<'bar'>>({ responsive: true, maintainAspectRatio: false });
  readonly lineOpts = signal<ChartOptions<'line'>>({ responsive: true, maintainAspectRatio: false });
  readonly doughnutOpts = signal<ChartOptions<'doughnut'>>({ responsive: true, maintainAspectRatio: false });
  readonly polarOpts = signal<ChartOptions<'polarArea'>>({ responsive: true, maintainAspectRatio: false });

  readonly surveyShowAvgChart = signal(false);
  readonly execShowAvgChart = signal(false);
  readonly execShowStatusCharts = signal(false);

  private static readonly fontFamily = `system-ui, "Segoe UI", sans-serif`;

  private readonly localeCharts = effect(() => {
    this.i18n.lang();
    const o = this.data();
    if (o?.surveyImpact) this.applySliceCharts(o.surveyImpact, 'survey');
    else this.clearSliceCharts('survey');
    if (o?.executionImpact) this.applySliceCharts(o.executionImpact, 'exec');
    else this.clearSliceCharts('exec');
    if (o && (o.surveyImpact || o.executionImpact)) {
      this.applySharedChartOpts();
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
    this.actionPlansApi.listPaged(1, 400).subscribe({
      next: (r) => this.planList.set([...r.items]),
      error: () => this.planList.set([]),
    });
    this.initiativesApi.listPaged(1, 400).subscribe({
      next: (r) => this.initiativeList.set([...r.items]),
      error: () => this.initiativeList.set([]),
    });
    this.load();
  }

  load(): void {
    this.busy.set(true);
    this.errorMessage.set(null);
    const filter = this.buildFilter();
    this.reportsApi.getImpactMeasurement(filter).subscribe({
      next: (d) => {
        this.data.set(d);
        this.busy.set(false);
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.data.set(null);
        const msg =
          err instanceof ApiBusinessError ? err.errors[0] ?? this.i18n.t('q.impact.error.load') : this.i18n.t('q.impact.error.load');
        this.errorMessage.set(msg);
      },
    });
  }

  applyFilters(): void {
    this.load();
  }

  resetFilters(): void {
    this.fromDate = '';
    this.toDate = '';
    this.splitAtLocal = '';
    this.selectedSurveyId = '';
    this.selectedPlanId = '';
    this.selectedInitiativeId = '';
    this.errorMessage.set(null);
    this.load();
  }

  planPickerLabel(p: ActionPlanDto): string {
    return qLocalizedTitle(this.i18n.lang(), p.titleAr ?? '', p.titleEn ?? '');
  }

  initiativePickerLabel(i: InitiativeListItemDto): string {
    const t = qLocalizedTitle(this.i18n.lang(), i.titleAr ?? '', i.titleEn ?? '');
    const plan = qLocalizedTitle(this.i18n.lang(), i.actionPlanTitleAr ?? '', i.actionPlanTitleEn ?? '');
    return `${t} — ${plan}`;
  }

  initiativeDistributionLabel(row: NamedCountDto): string {
    const map: Record<string, string> = {
      Planned: 'q.initiativeStatus.planned',
      InProgress: 'q.initiativeStatus.inProgress',
      Completed: 'q.initiativeStatus.completed',
      AtRisk: 'q.initiativeStatus.atRisk',
      Cancelled: 'q.initiativeStatus.cancelled',
    };
    const key = map[row.key];
    return key ? this.i18n.t(key) : row.key;
  }

  isSurveySlice(d: ImpactMeasurementDto): boolean {
    return d.scopeKind === 'Survey' || d.scopeKind === 'AllSurveys';
  }

  volumeLabel(d: ImpactMeasurementDto): string {
    return this.isSurveySlice(d) ? this.i18n.t('q.impact.chart.submissions') : this.i18n.t('q.impact.chart.progressEntries');
  }

  avgLabel(d: ImpactMeasurementDto): string {
    return this.isSurveySlice(d) ? this.i18n.t('q.impact.chart.avgRating') : this.i18n.t('q.impact.chart.avgProgress');
  }

  private buildFilter(): ImpactMeasurementFilterRequest {
    return {
      surveyId: this.selectedSurveyId?.trim() || undefined,
      actionPlanId: this.selectedPlanId?.trim() || undefined,
      initiativeId: this.selectedInitiativeId?.trim() || undefined,
      fromUtc: this.fromDate ? new Date(`${this.fromDate}T00:00:00.000Z`).toISOString() : undefined,
      toUtc: this.toDate ? new Date(`${this.toDate}T23:59:59.999Z`).toISOString() : undefined,
      splitAtUtc: this.splitAtLocal ? new Date(this.splitAtLocal).toISOString() : undefined,
    };
  }

  private palette() {
    const root = this.hostRef.nativeElement;
    const cs = getComputedStyle(root);
    const accent = (cs.getPropertyValue('--color-accent').trim() || '#6366f1').trim();
    const soft = (cs.getPropertyValue('--color-accent-soft').trim() || '#c7d2fe').trim();
    const text = (cs.getPropertyValue('--color-text').trim() || '#0f172a').trim();
    const muted = (cs.getPropertyValue('--color-muted').trim() || '#64748b').trim();
    const grid = 'rgba(148, 163, 184, 0.14)';
    const tooltipBg = 'rgba(15, 23, 42, 0.92)';
    return {
      before: soft,
      after: accent,
      sequence: ['#818cf8', '#38bdf8', '#34d399', '#fbbf24', '#f472b6', '#94a3b8'],
      text,
      muted,
      grid,
      tooltipBg,
      tooltipTitle: '#f8fafc',
      tooltipBody: '#e2e8f0',
    };
  }

  private clearSliceCharts(key: SliceKey): void {
    const emptyBar: ChartData<'bar'> = { labels: [], datasets: [] };
    const emptyLine: ChartData<'line'> = { labels: [], datasets: [] };
    const emptyDoughnut: ChartData<'doughnut'> = { labels: [], datasets: [] };
    const emptyPolar: ChartData<'polarArea'> = { labels: [], datasets: [] };
    if (key === 'survey') {
      this.surveyBarVolumeData.set(emptyBar);
      this.surveyBarAvgData.set(emptyBar);
      this.surveyBarAvgHorizontalData.set(emptyBar);
      this.surveyLineTrendData.set(emptyLine);
      this.surveyShowAvgChart.set(false);
    } else {
      this.execBarVolumeData.set(emptyBar);
      this.execBarAvgData.set(emptyBar);
      this.execBarAvgHorizontalData.set(emptyBar);
      this.execLineTrendData.set(emptyLine);
      this.execDoughnutStatusData.set(emptyDoughnut);
      this.execPolarStatusData.set(emptyPolar);
      this.execShowAvgChart.set(false);
      this.execShowStatusCharts.set(false);
    }
  }

  private applySliceCharts(d: ImpactMeasurementDto, key: SliceKey): void {
    const pal = this.palette();
    const beforeL = this.i18n.t('q.impact.before');
    const afterL = this.i18n.t('q.impact.after');
    const surveyLike = this.isSurveySlice(d);

    const volBefore = surveyLike ? d.before.surveySubmissionCount : d.before.progressEntryCount;
    const volAfter = surveyLike ? d.after.surveySubmissionCount : d.after.progressEntryCount;

    const volumeChart: ChartData<'bar'> = {
      labels: [beforeL, afterL],
      datasets: [
        {
          label: this.volumeLabel(d),
          data: [volBefore, volAfter],
          backgroundColor: [pal.before, pal.after],
          borderRadius: 8,
        },
      ],
    };

    const avgBefore = surveyLike ? d.before.averageRating : d.before.averageProgressPercent;
    const avgAfter = surveyLike ? d.after.averageRating : d.after.averageProgressPercent;
    const hasAvg = avgBefore != null || avgAfter != null;

    let barAvg: ChartData<'bar'> = { labels: [], datasets: [] };
    let barH: ChartData<'bar'> = { labels: [], datasets: [] };
    let lineT: ChartData<'line'> = { labels: [], datasets: [] };

    if (hasAvg) {
      const b = avgBefore ?? 0;
      const a = avgAfter ?? 0;
      barAvg = {
        labels: [beforeL, afterL],
        datasets: [
          {
            label: this.avgLabel(d),
            data: [b, a],
            backgroundColor: [pal.sequence[2], pal.sequence[4]],
            borderRadius: 8,
          },
        ],
      };
      barH = {
        labels: [beforeL, afterL],
        datasets: [
          {
            label: this.avgLabel(d),
            data: [b, a],
            backgroundColor: [pal.sequence[1], pal.sequence[3]],
            borderRadius: 8,
          },
        ],
      };
      const lineMetric = surveyLike ? this.i18n.t('q.impact.chart.avgRatingShort') : this.i18n.t('q.impact.chart.avgProgressShort');
      lineT = {
        labels: [beforeL, afterL],
        datasets: [
          {
            label: lineMetric,
            data: [b, a],
            borderColor: pal.after,
            backgroundColor: `${pal.after}33`,
            fill: true,
            tension: 0.35,
            pointRadius: 6,
          },
        ],
      };
    }

    const statusCount = d.initiativeStatusDistribution?.length ?? 0;
    const showStatus = statusCount > 0 && !surveyLike;

    let doughnut: ChartData<'doughnut'> = { labels: [], datasets: [] };
    let polar: ChartData<'polarArea'> = { labels: [], datasets: [] };
    if (showStatus) {
      const labels = d.initiativeStatusDistribution.map((x) => this.initiativeDistributionLabel(x));
      const values = d.initiativeStatusDistribution.map((x) => x.count);
      const colors = pal.sequence.slice(0, Math.max(labels.length, 1));
      doughnut = { labels, datasets: [{ data: values, backgroundColor: colors, borderWidth: 1 }] };
      polar = { labels, datasets: [{ data: values, backgroundColor: colors.map((c) => `${c}cc`), borderWidth: 1 }] };
    }

    if (key === 'survey') {
      this.surveyBarVolumeData.set(volumeChart);
      this.surveyBarAvgData.set(barAvg);
      this.surveyBarAvgHorizontalData.set(barH);
      this.surveyLineTrendData.set(lineT);
      this.surveyShowAvgChart.set(hasAvg);
    } else {
      this.execBarVolumeData.set(volumeChart);
      this.execBarAvgData.set(barAvg);
      this.execBarAvgHorizontalData.set(barH);
      this.execLineTrendData.set(lineT);
      this.execShowAvgChart.set(hasAvg);
      this.execDoughnutStatusData.set(doughnut);
      this.execPolarStatusData.set(polar);
      this.execShowStatusCharts.set(showStatus);
    }
  }

  private applySharedChartOpts(): void {
    const pal = this.palette();
    const commonLegend = {
      labels: { color: pal.text, font: { family: ImpactMeasurementPageComponent.fontFamily } },
    };
    const tooltip = {
      backgroundColor: pal.tooltipBg,
      titleColor: pal.tooltipTitle,
      bodyColor: pal.tooltipBody,
      borderColor: pal.grid,
      borderWidth: 1,
    };

    this.barOpts.set({
      responsive: true,
      maintainAspectRatio: false,
      plugins: { legend: commonLegend, tooltip },
      scales: {
        x: { ticks: { color: pal.muted, font: { family: ImpactMeasurementPageComponent.fontFamily } }, grid: { color: pal.grid } },
        y: {
          ticks: { color: pal.muted, font: { family: ImpactMeasurementPageComponent.fontFamily } },
          grid: { color: pal.grid },
          beginAtZero: true,
        },
      },
    });

    this.barHorizontalOpts.set({
      indexAxis: 'y',
      responsive: true,
      maintainAspectRatio: false,
      plugins: { legend: commonLegend, tooltip },
      scales: {
        x: {
          ticks: { color: pal.muted, font: { family: ImpactMeasurementPageComponent.fontFamily } },
          grid: { color: pal.grid },
          beginAtZero: true,
        },
        y: { ticks: { color: pal.muted, font: { family: ImpactMeasurementPageComponent.fontFamily } }, grid: { color: pal.grid } },
      },
    });

    this.lineOpts.set({
      responsive: true,
      maintainAspectRatio: false,
      plugins: { legend: commonLegend, tooltip },
      scales: {
        x: { ticks: { color: pal.muted, font: { family: ImpactMeasurementPageComponent.fontFamily } }, grid: { color: pal.grid } },
        y: {
          ticks: { color: pal.muted, font: { family: ImpactMeasurementPageComponent.fontFamily } },
          grid: { color: pal.grid },
          beginAtZero: true,
        },
      },
    });

    this.doughnutOpts.set({
      responsive: true,
      maintainAspectRatio: false,
      plugins: { legend: { ...commonLegend, position: 'bottom' as const }, tooltip },
    });

    this.polarOpts.set({
      responsive: true,
      maintainAspectRatio: false,
      plugins: { legend: { ...commonLegend, position: 'bottom' as const }, tooltip },
      scales: {
        r: {
          ticks: { color: pal.muted, backdropColor: 'transparent', font: { family: ImpactMeasurementPageComponent.fontFamily } },
          grid: { color: pal.grid },
        },
      },
    });
  }
}
