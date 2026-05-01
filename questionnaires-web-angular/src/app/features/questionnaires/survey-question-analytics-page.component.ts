import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SurveysApiService } from '../../services/surveys-api.service';
import { PagedResult } from '../../shared/models/api.types';
import { QuestionAnalyticsItemDto } from '../../shared/models/questionnaire.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

type SubpageViewMode = 'table' | 'cards';

@Component({
  selector: 'app-survey-question-analytics-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './survey-question-analytics-page.component.html',
  styleUrl: './survey-question-analytics-page.component.scss',
})
export class SurveyQuestionAnalyticsPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly surveysApi = inject(SurveysApiService);

  surveyId = '';
  page = 1;
  readonly pageSize = 25;

  readonly result = signal<PagedResult<QuestionAnalyticsItemDto> | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(false);

  readonly viewMode = signal<SubpageViewMode>('cards');

  setViewMode(m: SubpageViewMode): void {
    this.viewMode.set(m);
  }

  ngOnInit(): void {
    this.surveyId = this.route.snapshot.paramMap.get('surveyId') ?? '';
    this.load();
  }

  load(): void {
    if (!this.surveyId) return;
    this.busy.set(true);
    this.failed.set(false);
    this.surveysApi.getQuestionAnalytics(this.surveyId, this.page, this.pageSize).subscribe({
      next: (r) => {
        this.result.set(r);
        this.busy.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.busy.set(false);
      },
    });
  }

  nextPage(): void {
    const r = this.result();
    if (!r?.hasNextPage) return;
    this.page += 1;
    this.load();
  }

  prevPage(): void {
    if (this.page <= 1) return;
    this.page -= 1;
    this.load();
  }
}
