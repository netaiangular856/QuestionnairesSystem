import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ToastService } from '../../core/services/toast.service';
import { PublicSurveyApiService } from '../../services/public-survey-api.service';
import {
  AnswerUpsertDto,
  PublicSurveyPageDto,
  QuestionDto,
  QuestionType,
} from '../../shared/models/questionnaire.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

@Component({
  selector: 'app-public-survey-fill-page',
  standalone: true,
  imports: [FormsModule, TranslatePipe, RouterLink],
  templateUrl: './public-survey-fill-page.component.html',
  styleUrl: '../questionnaires/surveys/survey-fill-page/survey-fill-page.component.scss',
})
export class PublicSurveyFillPageComponent implements OnInit {
  private readonly publicApi = inject(PublicSurveyApiService);
  readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  readonly busy = signal(true);
  readonly submitting = signal(false);

  survey = signal<PublicSurveyPageDto | null>(null);
  questions = signal<QuestionDto[]>([]);
  answers: Record<string, unknown> = {};
  private surveyCode = '';

  readonly QuestionType = QuestionType;

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
    const code = this.route.snapshot.paramMap.get('code')?.trim();
    if (!code) {
      void this.router.navigate(['/portal']);
      return;
    }
    this.surveyCode = code;
    this.load(code);
  }

  private load(code: string): void {
    this.busy.set(true);
    forkJoin({
      survey: this.publicApi.getByCode(code),
      questions: this.publicApi.listQuestions(code),
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
        void this.router.navigate(['/portal']);
      },
    });
  }

  private initAnswers(qs: QuestionDto[]): void {
    for (const q of qs) {
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

  getOptions(q: QuestionDto): { value: string; labelAr: string; labelEn: string }[] {
    if (!q.optionsJson) return [];
    try {
      return JSON.parse(q.optionsJson);
    } catch {
      return [];
    }
  }

  onMultiChange(questionId: string, optionValue: string, event: Event): void {
    const target = event.target as HTMLInputElement;
    const current = this.answers[questionId] as string[];
    if (target.checked) {
      this.answers[questionId] = [...current, optionValue];
    } else {
      this.answers[questionId] = current.filter((v) => v !== optionValue);
    }
  }

  isMultiSelected(questionId: string, optionValue: string): boolean {
    const a = this.answers[questionId];
    return Array.isArray(a) && a.includes(optionValue);
  }

  ratingValue(questionId: string): number {
    const v = this.answers[questionId];
    return typeof v === 'number' ? v : 0;
  }

  formatAnswerValue(val: unknown): string {
    if (val === null || val === undefined) return '';
    if (Array.isArray(val)) return val.join(', ');
    if (typeof val === 'boolean') return val ? this.i18n.t('common.yes') : this.i18n.t('common.no');
    return String(val);
  }

  submit(): void {
    const survey = this.survey();
    if (!survey || !this.surveyCode) return;

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

    const answerPayload: AnswerUpsertDto[] = Object.keys(this.answers).map((qid) => ({
      questionId: qid,
      valueJson: JSON.stringify(this.answers[qid]),
    }));

    this.publicApi.createResponse(this.surveyCode, { answers: answerPayload }).subscribe({
      next: (resp) => {
        this.publicApi.submitResponse(this.surveyCode, resp.id).subscribe({
          next: () => {
            this.submitting.set(false);
            this.toast.show(this.i18n.t('public.fill.success'), 'success');
            void this.router.navigate(['/portal']);
          },
          error: () => {
            this.submitting.set(false);
            this.toast.show(this.i18n.t('q.fill.submitError'), 'error');
          },
        });
      },
      error: () => {
        this.submitting.set(false);
        this.toast.show(this.i18n.t('q.fill.saveError'), 'error');
      },
    });
  }
}
