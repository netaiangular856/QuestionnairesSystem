import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-athar-ai-thinking-loader',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <div class="athar-ai-thinking" role="status" [attr.aria-label]="label() | t">
      <span class="athar-ai-thinking__orb" aria-hidden="true"></span>
      <span class="athar-ai-thinking__dots" aria-hidden="true">
        <span></span><span></span><span></span>
      </span>
      <span class="athar-ai-thinking__text">{{ label() | t }}</span>
    </div>
  `,
  styles: `
    :host {
      display: block;
    }
    .athar-ai-thinking {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.75rem 1rem;
      border-radius: 12px;
      background: linear-gradient(120deg, rgba(99, 102, 241, 0.12), rgba(14, 165, 233, 0.08));
      border: 1px solid rgba(148, 163, 184, 0.25);
    }
    .athar-ai-thinking__orb {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: radial-gradient(circle at 30% 30%, #a5b4fc, #6366f1 60%, #312e81);
      box-shadow: 0 0 14px rgba(99, 102, 241, 0.55);
      animation: athar-pulse 1.4s ease-in-out infinite;
    }
    .athar-ai-thinking__dots {
      display: inline-flex;
      gap: 4px;
    }
    .athar-ai-thinking__dots span {
      width: 5px;
      height: 5px;
      border-radius: 50%;
      background: rgba(99, 102, 241, 0.85);
      animation: athar-bounce 1s ease-in-out infinite;
    }
    .athar-ai-thinking__dots span:nth-child(2) {
      animation-delay: 0.15s;
    }
    .athar-ai-thinking__dots span:nth-child(3) {
      animation-delay: 0.3s;
    }
    .athar-ai-thinking__text {
      font-size: 0.875rem;
      color: var(--athar-ai-muted, #64748b);
      font-weight: 500;
    }
    :host-context(.ai-drawer--dark) .athar-ai-thinking {
      background: linear-gradient(120deg, rgba(99, 102, 241, 0.2), rgba(14, 165, 233, 0.12));
      border-color: rgba(148, 163, 184, 0.28);
    }
    :host-context(.ai-drawer--dark) .athar-ai-thinking__text {
      color: #cbd5e1;
    }
    @keyframes athar-pulse {
      0%,
      100% {
        transform: scale(1);
        opacity: 1;
      }
      50% {
        transform: scale(1.15);
        opacity: 0.85;
      }
    }
    @keyframes athar-bounce {
      0%,
      80%,
      100% {
        transform: translateY(0);
        opacity: 0.35;
      }
      40% {
        transform: translateY(-4px);
        opacity: 1;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AtharAiThinkingLoaderComponent {
  readonly label = input<string>('atharAi.thinking');
}
