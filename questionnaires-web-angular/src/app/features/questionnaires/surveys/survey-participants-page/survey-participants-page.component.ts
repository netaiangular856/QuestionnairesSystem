import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SurveyParticipationApiService } from '../../../../services/survey-participation-api.service';
import { PagedResult } from '../../../../shared/models/api.types';
import { ParticipantDto } from '../../../../shared/models/questionnaire.models';
import { qParticipantStatusKey } from '../../../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';

type SubpageViewMode = 'table' | 'cards';

@Component({
  selector: 'app-survey-participants-page',
  standalone: true,
  imports: [RouterLink, DatePipe, TranslatePipe],
  templateUrl: './survey-participants-page.component.html',
  styleUrl: './survey-participants-page.component.scss',
})
export class SurveyParticipantsPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SurveyParticipationApiService);

  surveyId = '';
  page = 1;
  readonly pageSize = 25;

  readonly result = signal<PagedResult<ParticipantDto> | null>(null);
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
    this.api.listParticipants(this.surveyId, this.page, this.pageSize).subscribe({
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

  pStatus(row: ParticipantDto): string {
    return qParticipantStatusKey(row.status);
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
