import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SurveyParticipationApiService } from '../../../../services/survey-participation-api.service';
import { ParticipantDetailDto } from '../../../../shared/models/questionnaire.models';
import { qParticipantStatusKey, qResponseStatusKey } from '../../../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-survey-participant-detail-page',
  standalone: true,
  imports: [RouterLink, DatePipe, TranslatePipe],
  templateUrl: './survey-participant-detail-page.component.html',
  styleUrl: './survey-participant-detail-page.component.scss',
})
export class SurveyParticipantDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SurveyParticipationApiService);

  surveyId = '';
  participantId = '';

  readonly detail = signal<ParticipantDetailDto | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(true);

  ngOnInit(): void {
    this.surveyId = this.route.snapshot.paramMap.get('surveyId') ?? '';
    this.participantId = this.route.snapshot.paramMap.get('participantId') ?? '';
    this.load();
  }

  load(): void {
    if (!this.surveyId || !this.participantId) {
      this.busy.set(false);
      this.failed.set(true);
      return;
    }
    this.busy.set(true);
    this.failed.set(false);
    this.api.getParticipantDetail(this.surveyId, this.participantId).subscribe({
      next: (d) => {
        this.detail.set(d);
        this.busy.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.busy.set(false);
      },
    });
  }

  pStatusKey = qParticipantStatusKey;
  rStatusKey = qResponseStatusKey;

  formatAnswerValue(raw: string): string {
    const s = raw?.trim() ?? '';
    if (!s) return '—';
    try {
      return JSON.stringify(JSON.parse(s), null, 2);
    } catch {
      return s;
    }
  }
}
