import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ToastService } from '../../../../core/services/toast.service';
import { SurveysApiService } from '../../../../services/surveys-api.service';
import { SurveyQuestionsApiService } from '../../../../services/survey-questions-api.service';
import { SurveyParticipationApiService } from '../../../../services/survey-participation-api.service';
import {
  QuestionDto,
  QuestionType,
  SurveyDetailDto,
  AnswerUpsertDto,
  ParticipantDto,
} from '../../../../shared/models/questionnaire.models';
import { qLocalizedTitle } from '../../../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';

@Component({
  selector: 'app-survey-fill-page',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  templateUrl: './survey-fill-page.component.html',
  styleUrl: './survey-fill-page.component.scss',
})
export class SurveyFillPageComponent implements OnInit {
  private readonly surveysApi = inject(SurveysApiService);
  private readonly questionsApi = inject(SurveyQuestionsApiService);
  private readonly participationApi = inject(SurveyParticipationApiService);
  readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  readonly busy = signal(true);
  readonly submitting = signal(false);

  survey = signal<SurveyDetailDto | null>(null);
  questions = signal<QuestionDto[]>([]);
  answers: Record<string, any> = {};
  participant = signal<ParticipantDto | null>(null);

  readonly QuestionType = QuestionType;

  /** Calendar glyph for type="date" is often invisible with themed inputs / RTL — button opens native picker. */
  openNativeDatePicker(ev: Event): void {
    ev.preventDefault();
    ev.stopPropagation();
    const btn = ev.currentTarget as HTMLElement | null;
    const wrap = btn?.closest('.sv-datetime-wrap');
    const input = wrap?.querySelector('input[type="date"]') as
      | (HTMLInputElement & { showPicker?: () => void })
      | undefined;
    if (!input) return;
    if (typeof input.showPicker === 'function') {
      try {
        void input.showPicker();
        return;
      } catch {
        /* unsupported */
      }
    }
    input.focus();
    input.click();
  }

  ngOnInit(): void {
    const surveyId = this.route.snapshot.paramMap.get('surveyId');
    const participantId = this.route.snapshot.paramMap.get('participantId');
    const responseId = this.route.snapshot.paramMap.get('responseId');

    if (!surveyId) {
      void this.router.navigate(['/available-surveys']);
      return;
    }

    if (responseId) {
      this.loadResponseDetail(surveyId, responseId);
    } else if (participantId) {
      this.loadParticipantDetail(surveyId, participantId);
    } else {
      this.load(surveyId);
    }
  }

  private loadResponseDetail(surveyId: string, responseId: string): void {
    this.busy.set(true);
    forkJoin({
      survey: this.surveysApi.getById(surveyId),
      questions: this.questionsApi.list(surveyId),
      response: this.participationApi.getResponseById(responseId)
    }).subscribe({
      next: ({ survey, questions, response }) => {
        this.survey.set(survey);
        this.questions.set(questions.sort((a, b) => a.displayOrder - b.displayOrder));
        
        // Find participant info from response if possible
        if (response.participantId) {
           this.participationApi.getParticipantDetail(surveyId, response.participantId).subscribe(d => {
             this.participant.set(d.participant);
           });
        }

        this.initAnswers(questions);
        
        if (response.answers) {
          for (const ans of response.answers) {
            try {
              this.answers[ans.questionId] = JSON.parse(ans.valueJson);
            } catch {
              this.answers[ans.questionId] = ans.valueJson;
            }
          }
        }
        this.busy.set(false);
      },
      error: () => {
        this.busy.set(false);
        this.toast.show(this.i18n.t('q.fill.errorLoading'), 'error');
        void this.router.navigate(['/available-surveys']);
      }
    });
  }

  private loadParticipantDetail(surveyId: string, participantId: string): void {
    this.busy.set(true);
    forkJoin({
      survey: this.surveysApi.getById(surveyId),
      questions: this.questionsApi.list(surveyId),
      detail: this.participationApi.getParticipantDetail(surveyId, participantId)
    }).subscribe({
      next: ({ survey, questions, detail }) => {
        this.survey.set(survey);
        this.questions.set(questions.sort((a, b) => a.displayOrder - b.displayOrder));
        this.participant.set(detail.participant);
        this.initAnswers(questions);
        
        // If there's an existing response, populate answers
        if (detail.response && detail.response.answers) {
          for (const ans of detail.response.answers) {
            try {
              this.answers[ans.questionId] = JSON.parse(ans.valueJson);
            } catch {
              this.answers[ans.questionId] = ans.valueJson;
            }
          }
        }
        this.busy.set(false);
      },
      error: () => {
        this.busy.set(false);
        this.toast.show(this.i18n.t('q.fill.errorLoading'), 'error');
        void this.router.navigate(['/available-surveys']);
      }
    });
  }

  private load(id: string): void {
    this.busy.set(true);
    forkJoin({
      survey: this.surveysApi.getById(id),
      questions: this.questionsApi.list(id),
    }).subscribe({
      next: ({ survey, questions }) => {
        this.survey.set(survey);
        this.questions.set(questions.sort((a, b) => a.displayOrder - b.displayOrder));
        this.initAnswers(questions);
        this.busy.set(false);
      },
      error: () => {
        this.busy.set(false);
        this.toast.show(this.i18n.t('q.fill.errorLoading'), 'error');
        void this.router.navigate(['/available-surveys']);
      },
    });
  }

  private initAnswers(questions: QuestionDto[]): void {
    for (const q of questions) {
      if (q.type === QuestionType.MultipleChoice) {
        this.answers[q.id] = [];
      } else if (q.type === QuestionType.YesNo) {
        this.answers[q.id] = null;
      } else if (q.type === QuestionType.Rating || q.type === QuestionType.Scale) {
        this.answers[q.id] = 0;
      } else {
        this.answers[q.id] = '';
      }
    }
  }

  getLocalizedTitle(q: QuestionDto): string {
    return this.i18n.lang() === 'ar' ? q.titleAr : q.titleEn;
  }

  getLocalizedHelpText(q: QuestionDto): string {
    return (this.i18n.lang() === 'ar' ? q.helpTextAr : q.helpTextEn) || '';
  }

  getOptions(q: QuestionDto): any[] {
    if (!q.optionsJson) return [];
    try {
      return JSON.parse(q.optionsJson);
    } catch {
      return [];
    }
  }

  onMultiChange(questionId: string, optionValue: string, event: any): void {
    const current = this.answers[questionId] as string[];
    if (event.target.checked) {
      this.answers[questionId] = [...current, optionValue];
    } else {
      this.answers[questionId] = current.filter(v => v !== optionValue);
    }
  }

  formatAnswerValue(val: any): string {
    if (val === null || val === undefined) return '';
    if (Array.isArray(val)) return val.join(', ');
    if (typeof val === 'boolean') return val ? this.i18n.t('common.yes') : this.i18n.t('common.no');
    return String(val);
  }

  submit(): void {
    const survey = this.survey();
    if (!survey) return;

    // Validation
    for (const q of this.questions()) {
      if (q.isRequired) {
        const val = this.answers[q.id];
        if (val === undefined || val === null || val === '' || (Array.isArray(val) && val.length === 0)) {
          const msg = this.i18n.t('q.fill.requiredError').replace('{title}', this.getLocalizedTitle(q));
          this.toast.show(msg, 'error');
          return;
        }
      }
    }

    this.submitting.set(true);

    const answers: AnswerUpsertDto[] = Object.keys(this.answers).map(qid => ({
      questionId: qid,
      valueJson: JSON.stringify(this.answers[qid]),
    }));

    this.participationApi.createResponse(survey.id, { answers }).subscribe({
      next: (resp) => {
        this.participationApi.submitResponse(resp.id).subscribe({
          next: () => {
            this.submitting.set(false);
            this.toast.show(this.i18n.t('q.fill.success'), 'success');
            void this.router.navigate(['/available-surveys']);
          },
          error: () => {
            this.submitting.set(false);
            this.toast.show(this.i18n.t('q.fill.submitError'), 'error');
          }
        });
      },
      error: () => {
        this.submitting.set(false);
        this.toast.show(this.i18n.t('q.fill.saveError'), 'error');
      }
    });
  }
}
