import { DatePipe, NgClass } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActionPlansApiService } from '../../services/action-plans-api.service';
import { ActionPlanDto } from '../../shared/models/questionnaire.models';
import { qLocalizedTitle, qPlanStatusKey } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

@Component({
  selector: 'app-action-plans-page',
  standalone: true,
  imports: [DatePipe, TranslatePipe, NgClass],
  templateUrl: './action-plans-page.component.html',
  styleUrl: './action-plans-page.component.scss',
})
export class ActionPlansPageComponent implements OnInit {
  private readonly api = inject(ActionPlansApiService);
  readonly i18n = inject(I18nService);

  readonly items = signal<ActionPlanDto[]>([]);
  readonly failed = signal(false);
  readonly busy = signal(true);

  ngOnInit(): void {
    this.api.list().subscribe({
      next: (rows) => {
        this.items.set(rows);
        this.busy.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.busy.set(false);
      },
    });
  }

  titleOf(p: ActionPlanDto): string {
    return qLocalizedTitle(this.i18n.lang(), p.titleAr, p.titleEn);
  }

  statusKey(p: ActionPlanDto): string {
    return qPlanStatusKey(p.status);
  }

  planVariant(i: number): string {
    const v = i % 3;
    if (v === 0) return 'plan-card--slate';
    if (v === 1) return 'plan-card--clay';
    return 'plan-card--sky';
  }
}
