import { ChartData } from 'chart.js';
import { AiChartSuggestionDto } from '../models/questionnaire.models';

/** Semantic tones for answer-sentiment charts (order of `sentimentMix` doughnut segments: +, −, ~). */
export type SentimentTone = 'positive' | 'negative' | 'neutral';

const SENTIMENT: Record<
  SentimentTone,
  {
    /** Segment fill (with alpha) for doughnut / bars */
    fill: string;
    /** Stroke / border */
    stroke: string;
  }
> = {
  positive: { fill: 'rgba(34, 197, 94, 0.88)', stroke: '#15803d' },
  negative: { fill: 'rgba(239, 68, 68, 0.88)', stroke: '#b91c1c' },
  /** Neutral: amber (reads as “yellow” on light UI); distinct from red/green */
  neutral: { fill: 'rgba(234, 179, 8, 0.9)', stroke: '#a16207' },
};

const ORDER: SentimentTone[] = ['positive', 'negative', 'neutral'];

/** Classify a chart label (AR/EN or short AI tokens) to a sentiment tone, or null. */
export function classifySentimentLabel(raw: string): SentimentTone | null {
  const t = (raw ?? '').trim();
  if (!t) return null;
  const ar = t;
  const en = t.toLowerCase();

  if (ar.includes('إيجاب') || ar.includes('ايجاب')) return 'positive';
  if (ar.includes('سلب')) return 'negative';
  if (ar.includes('محايد')) return 'neutral';

  if (/\bvery\s+positive\b/.test(en) || /\bpositive\b/.test(en) || /\bpos\b/.test(en)) return 'positive';
  if (/\bvery\s+negative\b/.test(en) || /\bnegative\b/.test(en) || /\bneg\b/.test(en)) return 'negative';
  if (/\bneutral\b/.test(en) || /\bneu\b/.test(en)) return 'neutral';

  return null;
}

/** Doughnut dataset colors for fixed order: positive, negative, neutral. */
export function sentimentMixDoughnutColors(): { backgroundColor: string[]; borderColor: string[] } {
  return {
    backgroundColor: ORDER.map((k) => SENTIMENT[k].fill),
    borderColor: ORDER.map((k) => SENTIMENT[k].stroke),
  };
}

function barColorsForLabels(
  labels: string[],
  fallbackFill: string,
  fallbackStroke: string
): { backgroundColor: string | string[]; borderColor: string | string[] } {
  const tones = labels.map((l) => classifySentimentLabel(l));
  if (!tones.some((x) => x != null)) {
    return { backgroundColor: fallbackFill, borderColor: fallbackStroke };
  }
  return {
    backgroundColor: labels.map((_, i) => {
      const tone = tones[i];
      return tone ? SENTIMENT[tone].fill : fallbackFill;
    }),
    borderColor: labels.map((_, i) => {
      const tone = tones[i];
      return tone ? SENTIMENT[tone].stroke : fallbackStroke;
    }),
  };
}

function linePointColors(labels: string[]): { pointBackgroundColor: string[]; pointBorderColor: string[] } {
  return {
    pointBackgroundColor: labels.map((l) => {
      const tone = classifySentimentLabel(l);
      return tone ? SENTIMENT[tone].stroke : '#64748b';
    }),
    pointBorderColor: labels.map(() => '#ffffff'),
  };
}

/**
 * Chart.js data for the optional AI “insight” bar/line beside sentiment doughnut.
 * Uses green/red/amber when labels look like sentiment; otherwise keeps a neutral slate line / indigo bar fallback.
 */
export function buildSentimentInsightChartData(opts: {
  chart: AiChartSuggestionDto;
  kind: 'bar' | 'line';
  datasetLabel: string;
  /** Fallback bar fill when no label matches sentiment */
  fallbackBarFill?: string;
  fallbackBarStroke?: string;
}): ChartData<'bar' | 'line'> {
  const { chart, kind, datasetLabel } = opts;
  const labels = chart.labels ?? [];
  const values = (chart.values ?? []).map((v) => Number(v));
  const fallbackFill = opts.fallbackBarFill ?? 'rgba(99, 102, 241, 0.55)';
  const fallbackStroke = opts.fallbackBarStroke ?? '#6366f1';

  if (kind === 'bar') {
    const { backgroundColor, borderColor } = barColorsForLabels(labels, fallbackFill, fallbackStroke);
    return {
      labels,
      datasets: [
        {
          type: 'bar',
          data: values,
          label: datasetLabel,
          backgroundColor,
          borderColor,
          borderWidth: 1,
        },
      ],
    };
  }

  const pts = linePointColors(labels);
  return {
    labels,
    datasets: [
      {
        type: 'line',
        data: values,
        label: datasetLabel,
        borderColor: '#64748b',
        backgroundColor: 'rgba(148, 163, 184, 0.12)',
        tension: 0.25,
        fill: false,
        pointBackgroundColor: pts.pointBackgroundColor,
        pointBorderColor: pts.pointBorderColor,
        pointBorderWidth: 2,
        pointRadius: 5,
      },
    ],
  };
}
