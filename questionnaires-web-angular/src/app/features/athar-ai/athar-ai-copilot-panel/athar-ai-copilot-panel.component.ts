import { JsonPipe } from '@angular/common';
import { Component, EventEmitter, OnInit, Output, computed, inject, signal } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { Chart, ChartData, ChartOptions, TooltipOptions, registerables } from 'chart.js';
import { AuthService } from '../../../core/auth/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { AiApiService } from '../../../services/ai-api.service';
import { QuestionnaireLookupsApiService } from '../../../services/questionnaire-lookups-api.service';
import {
  AiAnalyzeReportsResponseDto,
  AiChartSuggestionDto,
  AiCopilotChatResponseDto,
  AiCopilotMessageDto,
  AiGeneratedSurveyDraftDto,
  AiInsightCardDto,
  AiKpiChipDto,
  AiSentimentAnalysisResponseDto,
  AiSuggestRecommendationDraftDto,
  LookupItemDto,
} from '../../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../../shared/models/permission-codes';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../shared/services/i18n.service';
import { extractApiErrorMessage } from '../../../shared/utils/api-helpers';
import {
  buildSentimentInsightChartData,
  sentimentMixDoughnutColors,
} from '../../../shared/charts/sentiment-chart-colors';
import { AtharAiInsightCardComponent } from '../components/athar-ai-insight-card.component';
import { AtharAiKpiRowComponent } from '../components/athar-ai-kpi-row.component';

Chart.register(...registerables);

type ChatBubble = {
  role: 'user' | 'assistant';
  text: string;
  html?: SafeHtml;
  typing?: boolean;
};

export type AtharAiPanelTab = 'chat' | 'scope' | 'insights';

@Component({
  selector: 'app-athar-ai-copilot-panel',
  standalone: true,
  imports: [
    JsonPipe,
    FormsModule,
    RouterLink,
    TranslatePipe,
    BaseChartDirective,
    AtharAiInsightCardComponent,
    AtharAiKpiRowComponent,
  ],
  templateUrl: './athar-ai-copilot-panel.component.html',
  styleUrl: './athar-ai-copilot-panel.component.scss',
})
export class AtharAiCopilotPanelComponent implements OnInit {
  private static readonly ChartPalette = ['#818cf8', '#22d3ee', '#a78bfa', '#34d399', '#f472b6', '#fbbf24', '#60a5fa'];

  private readonly aiApi = inject(AiApiService);
  private readonly lookupsApi = inject(QuestionnaireLookupsApiService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly sanitizer = inject(DomSanitizer);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  @Output() closed = new EventEmitter<void>();

  readonly canReports = this.auth.hasPermission(PermissionCodes.ReportView);
  readonly canSurveyManage = this.auth.hasPermission(PermissionCodes.SurveyManage);
  readonly canRecommendManage = this.auth.hasPermission(PermissionCodes.RecommendationManage);

  readonly activeTab = signal<AtharAiPanelTab>('chat');

  readonly surveyList = signal<LookupItemDto[]>([]);
  readonly busy = signal<'idle' | 'chat' | 'analyze' | 'sentiment' | 'survey' | 'autoSurvey' | 'rec'>('idle');
  readonly errorText = signal('');

  readonly analyzeResult = signal<AiAnalyzeReportsResponseDto | null>(null);
  readonly sentimentResult = signal<AiSentimentAnalysisResponseDto | null>(null);
  readonly overlayInsights = signal<AiInsightCardDto[]>([]);
  readonly recommendationDraft = signal<AiSuggestRecommendationDraftDto | null>(null);
  readonly surveyDraft = signal<AiGeneratedSurveyDraftDto | null>(null);

  readonly chat = signal<ChatBubble[]>([]);
  readonly draftInput = signal('');
  private pendingAssistantIndex: number | null = null;
  readonly showSurveyModal = signal(false);
  readonly surveyBriefAr = signal('');
  readonly surveyBriefEn = signal('');

  selectedSurveyId = '';
  fromDate = '';
  toDate = '';

  readonly copilotSuggestions = signal<string[]>([]);

  readonly suggestedPrompts = computed(() => {
    const dyn = this.copilotSuggestions();
    if (dyn.length > 0) return dyn;
    return [
      this.i18n.t('atharAi.sp.p1'),
      this.i18n.t('atharAi.sp.p2'),
      this.i18n.t('atharAi.sp.p3'),
      this.i18n.t('atharAi.sp.p4'),
    ];
  });

  readonly summarySafe = computed(() => this.safeHtml(this.pickBilingual(this.analyzeResult()?.summaryAr, this.analyzeResult()?.summaryEn)));
  readonly execSafe = computed(() => this.safeHtml(this.pickBilingual(this.analyzeResult()?.executiveBoxAr, this.analyzeResult()?.executiveBoxEn)));
  readonly sentimentSummarySafe = computed(() =>
    this.safeHtml(this.pickBilingual(this.sentimentResult()?.summaryAr, this.sentimentResult()?.summaryEn)),
  );

  readonly sentimentDoughnutData = computed<ChartData<'doughnut'> | null>(() => {
    const s = this.sentimentResult();
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

  readonly barChartOptions = computed<ChartOptions<'bar'>>(() => {
    this.i18n.lang();
    return this.tooltipOpts('bar');
  });
  readonly lineChartOptions = computed<ChartOptions<'line'>>(() => {
    this.i18n.lang();
    return this.tooltipOpts('line');
  });
  readonly doughnutChartOptions = computed<ChartOptions<'doughnut'>>(() => {
    this.i18n.lang();
    return this.tooltipOpts('doughnut');
  });

  ngOnInit(): void {
    if (!this.canReports) return;
    this.lookupsApi.getSurveys('', 500).subscribe({
      next: (list) => this.surveyList.set(list ?? []),
      error: () => this.surveyList.set([]),
    });
  }

  requestClose(): void {
    this.closed.emit();
  }

  setTab(tab: AtharAiPanelTab): void {
    this.activeTab.set(tab);
  }

  pickBilingual(ar?: string | null, en?: string | null): string {
    return this.i18n.lang() === 'ar' ? (ar ?? en ?? '') : (en ?? ar ?? '');
  }

  recommendationsList(): string[] {
    const r = this.analyzeResult();
    if (!r) return [];
    const list = this.i18n.lang() === 'ar' ? r.recommendationsAr : r.recommendationsEn;
    return Array.isArray(list) ? list.filter((x) => (x || '').trim()) : [];
  }

  actionPlanSteps(): string[] {
    const r = this.analyzeResult();
    if (!r) return [];
    const list = this.i18n.lang() === 'ar' ? r.actionPlanStepsAr : r.actionPlanStepsEn;
    return Array.isArray(list) ? list.filter((x) => (x || '').trim()) : [];
  }

  kpis(): AiKpiChipDto[] {
    const k = this.analyzeResult()?.kpis;
    return Array.isArray(k) ? k : [];
  }

  insightCards(): AiInsightCardDto[] {
    const over = this.overlayInsights();
    if (over.length > 0) return over;
    const a = this.analyzeResult()?.insightCards;
    return Array.isArray(a) ? a : [];
  }

  chartHeading(c: AiChartSuggestionDto): string {
    const ar = c.titleAr?.trim();
    const en = c.titleEn?.trim();
    const legacy = c.title?.trim();
    if (this.i18n.lang() === 'ar') return ar || en || legacy || '';
    return en || ar || legacy || '';
  }

  chartData(c: AiChartSuggestionDto): ChartData {
    const labels = c.labels ?? [];
    const values = (c.values ?? []).map((v) => Number(v));
    const palette = AtharAiCopilotPanelComponent.ChartPalette;
    const kind = (c.kind ?? 'bar').toLowerCase();
    const bg =
      kind === 'doughnut'
        ? labels.map((_, i) => `${palette[i % palette.length]}cc`)
        : 'rgba(129, 140, 248, 0.45)';
    const borderCol = kind === 'doughnut' ? labels.map((_, i) => palette[i % palette.length]) : '#818cf8';
    return {
      labels,
      datasets: [
        {
          data: values,
          label: this.chartHeading(c),
          backgroundColor: bg,
          borderColor: borderCol,
          borderWidth: kind === 'doughnut' ? 2 : 1,
        },
      ],
    };
  }

  sentimentInsightChartData(c: AiChartSuggestionDto): ChartData<'bar' | 'line'> {
    const kind = this.chartKind(c) === 'line' ? 'line' : 'bar';
    return buildSentimentInsightChartData({
      chart: c,
      kind,
      datasetLabel: this.chartHeading(c),
      fallbackBarFill: 'rgba(129, 140, 248, 0.45)',
      fallbackBarStroke: '#818cf8',
    });
  }

  chartKind(c: AiChartSuggestionDto): 'bar' | 'line' | 'doughnut' {
    const k = (c.kind ?? 'bar').toLowerCase();
    return k === 'line' || k === 'doughnut' ? k : 'bar';
  }

  thinkingLabel(): string {
    switch (this.busy()) {
      case 'chat':
        return 'atharAi.thinking.chat';
      case 'analyze':
        return 'atharAi.thinking.analyze';
      case 'sentiment':
        return 'atharAi.thinking.sentiment';
      case 'rec':
        return 'atharAi.thinking.rec';
      case 'survey':
        return 'atharAi.thinking.survey';
      case 'autoSurvey':
        return 'atharAi.thinking.autoSurvey';
      default:
        return 'atharAi.thinking';
    }
  }

  usePrompt(text: string): void {
    // Kept for legacy UX (fill input). Current UX prefers sendPrompt().
    this.draftInput.set(text);
    this.activeTab.set('chat');
  }

  sendPrompt(text: string): void {
    this.sendChat(text);
  }

  sendChat(overrideText?: string): void {
    if (!this.canReports) return;
    if (this.busy() !== 'idle') return;
    const raw = (overrideText ?? this.draftInput()).trim();
    if (!raw) {
      this.toast.show(this.i18n.t('atharAi.err.emptyMessage'), 'error');
      return;
    }
    this.errorText.set('');
    const historyBefore = this.toCopilotHistory();
    this.chat.update((c) => [...c, { role: 'user', text: raw }]);
    this.draftInput.set('');

    // Add an inline typing bubble (keeps UX inside chat — no "page loading").
    const idx = this.appendAssistantTyping();
    this.pendingAssistantIndex = idx;

    this.busy.set('chat');
    this.aiApi
      .copilotChat({
        surveyId: this.selectedSurveyId || undefined,
        analyticsFilter: this.buildFilter(),
        history: historyBefore,
        userMessage: raw,
      })
      .subscribe({
        next: (res) => this.onCopilotSuccess(res),
        error: (err) => this.onHttpError(err, 'atharAi.err.chat'),
      });
  }

  runAnalyze(): void {
    if (!this.canReports) return;
    this.errorText.set('');
    this.busy.set('analyze');
    this.aiApi.analyzeReports(this.buildFilter()).subscribe({
      next: (r) => {
        this.analyzeResult.set(r);
        this.overlayInsights.set([]);
        this.sentimentResult.set(null);
        this.busy.set('idle');
        this.appendAssistantFromHtml(this.pickBilingual(r.summaryAr, r.summaryEn));
        this.activeTab.set('chat');
      },
      error: (err) => this.onHttpError(err, 'atharAi.err.analyze'),
    });
  }

  runSentiment(): void {
    if (!this.canReports) return;
    this.errorText.set('');
    this.busy.set('sentiment');
    this.aiApi
      .sentimentAnalysis({
        surveyId: this.selectedSurveyId || undefined,
        analyticsFilter: this.buildFilter(),
        defaultWindowDays: 30,
      })
      .subscribe({
      next: (r) => {
        this.sentimentResult.set(r);
        this.overlayInsights.set(r.cards ?? []);
        this.busy.set('idle');
        const line = this.pickBilingual(r.overallToneAr, r.overallToneEn);
        const extra = line ? `<p><strong>${this.escapeHtml(line)}</strong></p>` : '';
        this.appendAssistantFromHtml(extra + this.pickBilingual(r.summaryAr, r.summaryEn));
        this.activeTab.set('chat');
      },
      error: (err) => this.onHttpError(err, 'atharAi.err.sentiment'),
    });
  }

  runRecommendation(): void {
    if (!this.canRecommendManage) return;
    this.errorText.set('');
    this.busy.set('rec');
    this.aiApi.suggestRecommendation({ surveyId: this.selectedSurveyId || undefined }).subscribe({
      next: (d) => {
        this.recommendationDraft.set(d);
        this.busy.set('idle');
        const body = this.pickBilingual(d.descriptionAr, d.descriptionEn);
        const html = `<p><strong>${this.escapeHtml(this.pickBilingual(d.titleAr, d.titleEn))}</strong></p><p>${this.escapeHtml(body || '')}</p>`;
        this.appendAssistantFromHtml(html);
        this.activeTab.set('chat');
      },
      error: (err) => this.onHttpError(err, 'atharAi.err.rec'),
    });
  }

  openSurveyModal(): void {
    this.showSurveyModal.set(true);
  }

  closeSurveyModal(): void {
    this.showSurveyModal.set(false);
  }

  runGenerateSurvey(): void {
    if (!this.canSurveyManage) return;
    const ar = this.surveyBriefAr().trim();
    const en = this.surveyBriefEn().trim();
    if (!ar && !en) {
      this.toast.show(this.i18n.t('atharAi.err.brief'), 'error');
      return;
    }
    this.busy.set('survey');
    this.aiApi.generateSurveyDraft({ briefAr: ar || undefined, briefEn: en || undefined }).subscribe({
      next: (d) => {
        this.surveyDraft.set(d);
        this.busy.set('idle');
        this.showSurveyModal.set(false);
        this.toast.show(this.i18n.t('atharAi.survey.done'), 'success');
        this.activeTab.set('chat');
      },
      error: (err) => this.onHttpError(err, 'atharAi.err.survey'),
    });
  }

  /** AI reads recommendations + recent surveys, creates a draft survey in the system (no manual brief). */
  runAutoSurveyFromRecommendations(): void {
    if (!this.canSurveyManage) return;
    this.errorText.set('');
    this.busy.set('autoSurvey');
    this.aiApi.generateSurveyFromRecommendations({}).subscribe({
      next: (r) => {
        this.busy.set('idle');
        this.toast.show(this.i18n.t('atharAi.toast.autoSurveyOk'), 'success');
        void this.router.navigate(['/surveys', r.surveyId]);
        this.requestClose();
      },
      error: (err) => this.onHttpError(err, 'atharAi.err.autoSurvey'),
    });
  }

  private buildFilter() {
    const fromUtc = this.fromDate ? localDateStartUtc(this.fromDate) : undefined;
    const toUtc = this.toDate ? localDateEndUtc(this.toDate) : undefined;
    return {
      surveyId: this.selectedSurveyId || undefined,
      fromUtc,
      toUtc,
    };
  }

  private toCopilotHistory(): AiCopilotMessageDto[] {
    return this.chat().map((m) => ({
      role: m.role,
      content: m.text,
    }));
  }

  private onCopilotSuccess(res: AiCopilotChatResponseDto): void {
    this.busy.set('idle');
    this.overlayInsights.set(res.insightCards ?? []);
    const sug = this.i18n.lang() === 'ar' ? res.suggestedPromptsAr : res.suggestedPromptsEn;
    this.copilotSuggestions.set(Array.isArray(sug) ? sug.filter((s) => (s || '').trim()) : []);

    const html = this.pickBilingual(res.replyAr, res.replyEn);
    this.replacePendingAssistantHtml(html);

    // If AI returned structured insights, keep them in the chat too (no tab jump).
    if ((res.insightCards?.length ?? 0) > 0) {
      this.appendAssistantFromHtml(this.renderInsightCardsAsHtml(res.insightCards), false);
    }

    this.activeTab.set('chat');
  }

  private appendAssistantTyping(): number {
    const bubble: ChatBubble = { role: 'assistant', text: '', typing: true };
    const index = this.chat().length;
    this.chat.update((c) => [...c, bubble]);
    return index;
  }

  private replacePendingAssistantHtml(html: string): void {
    const idx = this.pendingAssistantIndex;
    this.pendingAssistantIndex = null;
    const trimmed = (html || '').trim();
    if (idx == null || idx < 0) {
      if (trimmed) this.appendAssistantFromHtml(trimmed, true);
      return;
    }
    this.chat.update((list) => {
      const copy = [...list];
      const b = copy[idx];
      if (!b || b.role !== 'assistant') return copy;
      copy[idx] = {
        role: 'assistant',
        text: AtharAiCopilotPanelComponent.stripTags(trimmed),
        html: this.sanitizer.bypassSecurityTrustHtml(trimmed),
        typing: false,
      };
      return copy;
    });
  }

  private renderInsightCardsAsHtml(cards: AiInsightCardDto[]): string {
    const list = Array.isArray(cards) ? cards.slice(0, 6) : [];
    if (list.length === 0) return '';
    const title = this.i18n.t('atharAi.panel.cards');
    const items = list
      .map((c) => {
        const t = this.escapeHtml(this.pickBilingual(c.titleAr, c.titleEn));
        const b = this.escapeHtml(this.pickBilingual(c.bodyAr, c.bodyEn));
        return `<li><strong>${t}</strong><br/>${b}</li>`;
      })
      .join('');
    return `<h4>${this.escapeHtml(title)}</h4><ul>${items}</ul>`;
  }

  private appendAssistantFromHtml(html: string, typingPreferred = false): void {
    const trimmed = (html || '').trim();
    if (!trimmed) return;
    const plain = AtharAiCopilotPanelComponent.stripTags(trimmed);
    const useTyping = typingPreferred && !trimmed.includes('<');
    const bubble: ChatBubble = {
      role: 'assistant',
      text: useTyping ? '' : plain,
      html: useTyping ? undefined : this.sanitizer.bypassSecurityTrustHtml(trimmed),
      typing: useTyping,
    };
    this.chat.update((c) => [...c, bubble]);
    if (useTyping) {
      this.runPlainTyping(trimmed, this.chat().length - 1);
    }
  }

  private runPlainTyping(full: string, index: number): void {
    let i = 0;
    const step = 2;
    const tick = () => {
      i = Math.min(full.length, i + step);
      const slice = full.slice(0, i);
      this.chat.update((list) => {
        const copy = [...list];
        const b = copy[index];
        if (b && b.role === 'assistant') {
          const done = i >= full.length;
          copy[index] = {
            ...b,
            text: slice,
            typing: !done,
            html: done ? this.sanitizer.bypassSecurityTrustHtml(`<p>${this.escapeHtml(full)}</p>`) : undefined,
          };
        }
        return copy;
      });
      if (i < full.length) {
        window.setTimeout(tick, 14);
      }
    };
    tick();
  }

  private static stripTags(s: string): string {
    return s
      .replace(/<[^>]+>/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();
  }

  private onHttpError(err: unknown, fallbackKey: string): void {
    this.busy.set('idle');
    const msg = extractApiErrorMessage(err, this.i18n.t(fallbackKey));
    this.errorText.set(msg);
    this.toast.show(msg, 'error');

    // If we had a pending typing bubble, replace it with the error (keeps failure inside chat).
    if (this.pendingAssistantIndex != null) {
      const html = `<p><strong>${this.escapeHtml(msg)}</strong></p>`;
      this.replacePendingAssistantHtml(html);
    }
  }

  private safeHtml(raw: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(raw || '');
  }

  private escapeHtml(s: string): string {
    return (s || '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  private tooltipOpts<K extends 'bar' | 'line' | 'doughnut'>(_kind: K): ChartOptions<K> {
    const rtl = this.i18n.isRtl();
    const tooltip: TooltipOptions<K> = {
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
    } as TooltipOptions<K>;
    return {
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      layout: { padding: { top: 10, right: 6, bottom: 8, left: 6 } },
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { display: false },
        tooltip,
      },
    } as ChartOptions<K>;
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
