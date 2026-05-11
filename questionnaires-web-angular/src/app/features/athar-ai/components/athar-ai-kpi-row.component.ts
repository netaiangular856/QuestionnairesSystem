import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { AiKpiChipDto } from '../../../shared/models/questionnaire.models';
import { I18nService } from '../../../shared/services/i18n.service';

@Component({
  selector: 'app-athar-ai-kpi-row',
  standalone: true,
  template: `
    @if (kpis().length) {
      <div class="athar-kpi-row" role="list">
        @for (k of kpis(); track $index) {
          <div class="athar-kpi" role="listitem">
            <span class="athar-kpi__label">{{ label(k) }}</span>
            <span class="athar-kpi__value">{{ k.valueText }}</span>
            @if (hint(k)) {
              <span class="athar-kpi__hint">{{ hint(k) }}</span>
            }
          </div>
        }
      </div>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    .athar-kpi-row {
      display: flex;
      flex-wrap: wrap;
      gap: 0.65rem;
    }
    .athar-kpi {
      min-width: 120px;
      flex: 1 1 140px;
      padding: 0.65rem 0.85rem;
      border-radius: 12px;
      background: rgba(255, 255, 255, 0.65);
      border: 1px solid rgba(148, 163, 184, 0.2);
      box-shadow: 0 4px 16px rgba(15, 23, 42, 0.04);
    }
    .athar-kpi__label {
      display: block;
      font-size: 0.72rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: #64748b;
      margin-bottom: 0.25rem;
    }
    .athar-kpi__value {
      display: block;
      font-size: 1.1rem;
      font-weight: 700;
      color: #1e1b4b;
      letter-spacing: -0.02em;
    }
    .athar-kpi__hint {
      display: block;
      margin-top: 0.25rem;
      font-size: 0.75rem;
      color: #64748b;
      line-height: 1.35;
    }
    :host-context(.ai-drawer--dark) .athar-kpi {
      background: rgba(30, 41, 59, 0.75);
      border-color: rgba(148, 163, 184, 0.2);
    }
    :host-context(.ai-drawer--dark) .athar-kpi__label {
      color: #94a3b8;
    }
    :host-context(.ai-drawer--dark) .athar-kpi__value {
      color: #f8fafc;
    }
    :host-context(.ai-drawer--dark) .athar-kpi__hint {
      color: #94a3b8;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AtharAiKpiRowComponent {
  private readonly i18n = inject(I18nService);

  readonly kpis = input<AiKpiChipDto[]>([]);

  readonly ar = computed(() => this.i18n.lang() === 'ar');

  label(k: AiKpiChipDto): string {
    return this.ar() ? k.labelAr : k.labelEn;
  }

  hint(k: AiKpiChipDto): string {
    const h = this.ar() ? k.hintAr : k.hintEn;
    return (h || '').trim();
  }
}
