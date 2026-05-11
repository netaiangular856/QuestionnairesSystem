import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { Chart, ChartData, ChartOptions, TooltipOptions, registerables } from 'chart.js';
import { AuthService } from '../../../../core/auth/auth.service';
import { AiApiService } from '../../../../services/ai-api.service';
import { QuestionnaireLookupsApiService } from '../../../../services/questionnaire-lookups-api.service';
import {
  AiAnalyzeReportsResponseDto,
  AiChartSuggestionDto,
  LookupItemDto,
} from '../../../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../../../shared/models/permission-codes';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';
import { ApiBusinessError } from '../../../../shared/utils/api-helpers';
import { ToastService } from '../../../../core/services/toast.service';

Chart.register(...registerables);

@Component({
  selector: 'app-ai-analysis-page',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe, BaseChartDirective],
  templateUrl: './ai-analysis-page.component.html',
  styleUrl: './ai-analysis-page.component.scss',
})
export class AiAnalysisPageComponent implements OnInit {
  /** Palette tuned for light backgrounds (aligned with Reports). */
  private static readonly ChartPalette = [
    '#6366f1',
    '#818cf8',
    '#38bdf8',
    '#22c55e',
    '#f59e0b',
    '#ec4899',
    '#8b5cf6',
  ];

  private readonly aiApi = inject(AiApiService);
  private readonly lookupsApi = inject(QuestionnaireLookupsApiService);
  private readonly toast = inject(ToastService);
  private readonly sanitizer = inject(DomSanitizer);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  readonly canViewReports = this.auth.hasPermission(PermissionCodes.ReportView);

  readonly surveyList = signal<LookupItemDto[]>([]);
  readonly busy = signal(false);
  readonly analyzeBusy = signal(false);
  readonly failed = signal(false);
  readonly errorText = signal('');
  readonly result = signal<AiAnalyzeReportsResponseDto | null>(null);

  fromDate = '';
  toDate = '';
  selectedSurveyId = '';

  readonly summarySafe = computed<SafeHtml>(() => {
    const r = this.result();
    if (!r) return '';
    const raw = this.i18n.lang() === 'ar' ? r.summaryAr : r.summaryEn;
    return this.sanitizer.bypassSecurityTrustHtml(raw ?? '');
  });

  /** Rebuilt when language toggles (tooltip RTL + labels). */
  readonly barChartOptions = computed<ChartOptions<'bar'>>(() => {
    this.i18n.lang();
    return this.buildBarChartOptions();
  });
  readonly lineChartOptions = computed<ChartOptions<'line'>>(() => {
    this.i18n.lang();
    return this.buildLineChartOptions();
  });
  readonly doughnutChartOptions = computed<ChartOptions<'doughnut'>>(() => {
    this.i18n.lang();
    return this.buildDoughnutChartOptions();
  });

  ngOnInit(): void {
    if (!this.canViewReports) return;
    this.lookupsApi.getSurveys('', 500).subscribe({
      next: (list) => this.surveyList.set(list ?? []),
      error: () => this.surveyList.set([]),
    });
  }

  /** Localized chart title (API bilingual + known English fallbacks for legacy AI output). */
  chartHeading(c: AiChartSuggestionDto): string {
    const ar = c.titleAr?.trim();
    const en = c.titleEn?.trim();
    const legacy = c.title?.trim();
    if (this.i18n.lang() === 'ar') {
      if (ar) return ar;
      const mapped = this.mapKnownChartTitle(en || legacy);
      if (mapped) return mapped;
      return en || legacy || '';
    }
    if (en) return en;
    const mapped = this.mapKnownChartTitle(ar || legacy);
    if (mapped) return mapped;
    return ar || legacy || '';
  }

  private normalizeChartTitleKey(raw?: string | null): string {
    return (raw || '')
      .toLowerCase()
      .replace(/\bthe\b/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();
  }

  private mapKnownChartTitle(raw?: string | null): string | undefined {
    const n = this.normalizeChartTitleKey(raw);
    const keys: Record<string, string> = {
      'survey status distribution': 'q.ai.chart.surveyStatus',
      'response status distribution': 'q.ai.chart.responseStatus',
      'submissions by day of week': 'q.ai.chart.submissionsWeekday',
      'submissions by weekday': 'q.ai.chart.submissionsWeekday',
      'ratings distribution': 'q.ai.chart.ratings',
    };
    const key = keys[n];
    return key ? this.i18n.t(key) : undefined;
  }

  private aiTooltipOptions<K extends 'bar' | 'line' | 'doughnut'>(rtl: boolean): TooltipOptions<K> {
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
    } as TooltipOptions<K>;
  }

  private buildBarChartOptions(): ChartOptions<'bar'> {
    const rtl = this.i18n.isRtl();
    return {
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      layout: { padding: { top: 12, right: 8, bottom: 10, left: 8 } },
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { display: false },
        tooltip: this.aiTooltipOptions<'bar'>(rtl),
      },
    };
  }

  private buildLineChartOptions(): ChartOptions<'line'> {
    const rtl = this.i18n.isRtl();
    return {
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      layout: { padding: { top: 12, right: 8, bottom: 10, left: 8 } },
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { display: false },
        tooltip: this.aiTooltipOptions<'line'>(rtl),
      },
    };
  }

  private buildDoughnutChartOptions(): ChartOptions<'doughnut'> {
    const rtl = this.i18n.isRtl();
    return {
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      layout: { padding: { top: 12, right: 8, bottom: 10, left: 8 } },
      plugins: {
        legend: { display: false },
        tooltip: this.aiTooltipOptions<'doughnut'>(rtl),
      },
    };
  }

  chartData(c: AiChartSuggestionDto): ChartData {
    const labels = c.labels ?? [];
    const values = (c.values ?? []).map((v) => Number(v));
    const palette = AiAnalysisPageComponent.ChartPalette;
    const bg =
      c.kind === 'doughnut'
        ? labels.map((_, i) => {
            const hex = palette[i % palette.length];
            return `${hex}bf`;
          })
        : 'rgba(99, 102, 241, 0.55)';
    const borderCol =
      c.kind === 'doughnut'
        ? labels.map((_, i) => palette[i % palette.length])
        : '#6366f1';
    return {
      labels,
      datasets: [
        {
          data: values,
          label: this.chartHeading(c),
          backgroundColor: bg,
          borderColor: borderCol,
          borderWidth: c.kind === 'doughnut' ? 2 : 1,
        },
      ],
    };
  }

  chartKind(c: AiChartSuggestionDto): 'bar' | 'line' | 'doughnut' {
    const k = (c.kind ?? 'bar').toLowerCase();
    return k === 'line' || k === 'doughnut' ? k : 'bar';
  }

  runAnalysis(): void {
    if (!this.canViewReports) return;
    this.analyzeBusy.set(true);
    this.failed.set(false);
    this.errorText.set('');
    this.result.set(null);

    const fromUtc = this.fromDate ? localDateStartUtc(this.fromDate) : undefined;
    const toUtc = this.toDate ? localDateEndUtc(this.toDate) : undefined;

    this.aiApi
      .analyzeReports({
        surveyId: this.selectedSurveyId || undefined,
        fromUtc,
        toUtc,
      })
      .subscribe({
        next: (r) => {
          this.result.set(r);
          this.analyzeBusy.set(false);
        },
        error: (err: unknown) => {
          this.analyzeBusy.set(false);
          this.failed.set(true);
          const msg =
            err instanceof ApiBusinessError && err.errors.length > 0
              ? err.errors[0]
              : this.i18n.t('q.ai.analyze.error');
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
