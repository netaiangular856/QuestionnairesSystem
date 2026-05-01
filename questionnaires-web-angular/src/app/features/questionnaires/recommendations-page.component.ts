import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RecommendationsApiService } from '../../services/recommendations-api.service';
import { RecommendationDto } from '../../shared/models/questionnaire.models';
import { qLocalizedTitle, qRecStatusKey } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

@Component({
  selector: 'app-recommendations-page',
  standalone: true,
  imports: [DatePipe, TranslatePipe],
  templateUrl: './recommendations-page.component.html',
  styleUrl: './recommendations-page.component.scss',
})
export class RecommendationsPageComponent implements OnInit {
  private readonly api = inject(RecommendationsApiService);
  readonly i18n = inject(I18nService);

  readonly items = signal<RecommendationDto[]>([]);
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

  titleOf(r: RecommendationDto): string {
    return qLocalizedTitle(this.i18n.lang(), r.titleAr, r.titleEn);
  }

  statusKey(r: RecommendationDto): string {
    return qRecStatusKey(r.status);
  }
}
