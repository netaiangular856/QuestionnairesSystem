import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { AiInsightCardDto } from '../../../shared/models/questionnaire.models';
import { I18nService } from '../../../shared/services/i18n.service';

@Component({
  selector: 'app-athar-ai-insight-card',
  standalone: true,
  template: `
    <article class="athar-insight" [class]="kindClass()">
      <div class="athar-insight__accent"></div>
      <div class="athar-insight__body">
        <h4 class="athar-insight__title">{{ title() }}</h4>
        <p class="athar-insight__text">{{ body() }}</p>
        @if (severityLabel()) {
          <span class="athar-insight__sev">{{ severityLabel() }}</span>
        }
      </div>
    </article>
  `,
  styles: `
    :host {
      display: block;
    }
    .athar-insight {
      position: relative;
      display: flex;
      gap: 0.75rem;
      padding: 1rem 1rem 1rem 0.85rem;
      border-radius: 14px;
      /* Opaque card: backdrop-filter on many stacked rows is very expensive while scrolling. */
      background: rgba(255, 255, 255, 0.94);
      border: 1px solid rgba(148, 163, 184, 0.22);
      box-shadow: 0 8px 28px rgba(15, 23, 42, 0.06);
      overflow: hidden;
      animation: athar-card-in 0.45s ease-out both;
    }
    .athar-insight__accent {
      width: 4px;
      border-radius: 4px;
      flex-shrink: 0;
      background: linear-gradient(180deg, #6366f1, #0ea5e9);
    }
    .athar-insight--risk .athar-insight__accent {
      background: linear-gradient(180deg, #f97316, #dc2626);
    }
    .athar-insight--low .athar-insight__accent {
      background: linear-gradient(180deg, #fbbf24, #d97706);
    }
    .athar-insight--sentiment .athar-insight__accent {
      background: linear-gradient(180deg, #a78bfa, #6366f1);
    }
    .athar-insight--action .athar-insight__accent {
      background: linear-gradient(180deg, #22c55e, #059669);
    }
    .athar-insight__title {
      margin: 0 0 0.35rem;
      font-size: 0.95rem;
      font-weight: 650;
      color: #0f172a;
    }
    .athar-insight__text {
      margin: 0;
      font-size: 0.85rem;
      line-height: 1.5;
      color: #475569;
    }
    .athar-insight__sev {
      display: inline-block;
      margin-top: 0.5rem;
      font-size: 0.7rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      padding: 0.15rem 0.45rem;
      border-radius: 6px;
      background: rgba(99, 102, 241, 0.12);
      color: #4338ca;
    }
    :host-context(.ai-drawer--dark) .athar-insight {
      background: rgba(30, 41, 59, 0.72);
      border-color: rgba(148, 163, 184, 0.18);
      box-shadow: 0 8px 28px rgba(0, 0, 0, 0.25);
    }
    :host-context(.ai-drawer--dark) .athar-insight__title {
      color: #f1f5f9;
    }
    :host-context(.ai-drawer--dark) .athar-insight__text {
      color: #cbd5e1;
    }
    :host-context(.ai-drawer--dark) .athar-insight__sev {
      background: rgba(129, 140, 248, 0.2);
      color: #c7d2fe;
    }
    @keyframes athar-card-in {
      from {
        opacity: 0;
        transform: translateY(6px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AtharAiInsightCardComponent {
  private readonly i18n = inject(I18nService);

  readonly card = input.required<AiInsightCardDto>();

  readonly title = computed(() => {
    const c = this.card();
    return this.i18n.lang() === 'ar' ? c.titleAr : c.titleEn;
  });

  readonly body = computed(() => {
    const c = this.card();
    return this.i18n.lang() === 'ar' ? c.bodyAr : c.bodyEn;
  });

  readonly kindClass = computed(() => {
    const k = (this.card().kind || '').toLowerCase();
    if (k === 'risk_detected') return 'athar-insight athar-insight--risk';
    if (k === 'low_satisfaction') return 'athar-insight athar-insight--low';
    if (k === 'sentiment_summary') return 'athar-insight athar-insight--sentiment';
    if (k === 'recommended_action') return 'athar-insight athar-insight--action';
    return 'athar-insight';
  });

  readonly severityLabel = computed(() => {
    const s = (this.card().severity || '').toLowerCase();
    if (!s) return '';
    const key =
      s === 'high' ? 'atharAi.sev.high' : s === 'medium' ? 'atharAi.sev.medium' : s === 'low' ? 'atharAi.sev.low' : '';
    return key ? this.i18n.t(key) : this.card().severity || '';
  });
}
