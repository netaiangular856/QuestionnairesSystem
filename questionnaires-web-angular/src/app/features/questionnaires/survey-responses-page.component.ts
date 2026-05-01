import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SurveyParticipationApiService } from '../../services/survey-participation-api.service';
import { PagedResult } from '../../shared/models/api.types';
import { ResponseListItemDto } from '../../shared/models/questionnaire.models';
import { qResponseStatusKey } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

type SubpageViewMode = 'table' | 'cards';

@Component({
  selector: 'app-survey-responses-page',
  standalone: true,
  imports: [RouterLink, DatePipe, TranslatePipe],
  templateUrl: './survey-responses-page.component.html',
  styleUrl: './survey-responses-page.component.scss',
})
export class SurveyResponsesPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SurveyParticipationApiService);

  surveyId = '';
  page = 1;
  readonly pageSize = 25;

  readonly result = signal<PagedResult<ResponseListItemDto> | null>(null);
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
    this.api.listResponses(this.surveyId, this.page, this.pageSize).subscribe({
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

  rStatus(row: ResponseListItemDto): string {
    return qResponseStatusKey(row.status);
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
