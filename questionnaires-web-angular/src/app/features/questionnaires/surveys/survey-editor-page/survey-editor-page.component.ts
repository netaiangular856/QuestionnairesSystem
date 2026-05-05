import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastService } from '../../../../core/services/toast.service';
import { SurveysApiService } from '../../../../services/surveys-api.service';
import { SurveyQuestionsApiService } from '../../../../services/survey-questions-api.service';
import {
  CreateSurveyQuestionItem,
  QuestionDto,
  QuestionType,
  SurveyAudienceMemberInputDto,
  SurveyAudiencePickItem,
  SurveyAudienceScope,
  SurveyAudienceSubjectKind,
  UpdateSurveyRequest,
} from '../../../../shared/models/questionnaire.models';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';
import { SurveyAudiencePickerComponent } from '../../../../shared/questionnaires/survey-audience-picker/survey-audience-picker.component';
import { PublicArticleEditorComponent } from '../../../../shared/questionnaires/public-article-editor/public-article-editor.component';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-survey-editor-page',
  standalone: true,
  imports: [FormsModule, TranslatePipe, SurveyAudiencePickerComponent, PublicArticleEditorComponent],
  templateUrl: './survey-editor-page.component.html',
  styleUrl: './survey-editor-page.component.scss',
})
export class SurveyEditorPageComponent implements OnInit {
  private readonly api = inject(SurveysApiService);
  private readonly questionsApi = inject(SurveyQuestionsApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  readonly busy = signal(false);
  readonly saveBusy = signal(false);
  readonly step = signal(0);
  private surveyId: string | null = null;

  titleAr = '';
  titleEn = '';
  descriptionAr = '';
  descriptionEn = '';
  code = '';
  showOnPublicPortal = false;
  publicArticleEnabled = false;
  publicArticleTitleAr = '';
  publicArticleTitleEn = '';
  publicArticleBodyAr = '';
  publicArticleBodyEn = '';
  audienceScope = SurveyAudienceScope.AllOrganizationMembers;
  audienceSelection: SurveyAudiencePickItem[] = [];
  questions: CreateSurveyQuestionItem[] = [this.emptyQuestion()];

  readonly QuestionType = QuestionType;
  readonly SurveyAudienceScope = SurveyAudienceScope;

  readonly questionTypes: { value: QuestionType; labelKey: string }[] = [
    { value: QuestionType.ShortText, labelKey: 'q.templates.qt.shortText' },
    { value: QuestionType.LongText, labelKey: 'q.templates.qt.longText' },
    { value: QuestionType.SingleChoice, labelKey: 'q.templates.qt.single' },
    { value: QuestionType.MultipleChoice, labelKey: 'q.templates.qt.multi' },
    { value: QuestionType.Rating, labelKey: 'q.templates.qt.rating' },
    { value: QuestionType.Scale, labelKey: 'q.templates.qt.scale' },
    { value: QuestionType.YesNo, labelKey: 'q.templates.qt.yesno' },
    { value: QuestionType.Date, labelKey: 'q.templates.qt.date' },
    { value: QuestionType.Number, labelKey: 'q.templates.qt.number' },
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('surveyId');
    if (id) {
      this.surveyId = id;
      this.loadSurvey(id);
    } else {
      void this.router.navigate(['/surveys']);
    }
  }

  stepLabelKey(i: number): string {
    const keys = ['q.surveys.wizard.stepMeta', 'q.surveys.wizard.stepQuestions'] as const;
    return keys[i] ?? keys[0];
  }

  emptyQuestion(): CreateSurveyQuestionItem {
    return {
      type: QuestionType.ShortText,
      titleAr: '',
      titleEn: '',
      helpTextAr: null,
      helpTextEn: null,
      isRequired: false,
      optionsJson: null,
      displayOrder: null,
    };
  }

  addQuestion(): void {
    this.questions = [...this.questions, this.emptyQuestion()];
  }

  removeQuestion(index: number): void {
    if (this.questions.length <= 1) return;
    this.questions = this.questions.filter((_, i) => i !== index);
  }

  prev(): void {
    const s = this.step();
    if (s <= 0) {
      this.cancel();
      return;
    }
    this.step.set(s - 1);
  }

  onAudienceScopeChange(): void {
    if (this.audienceScope !== SurveyAudienceScope.SpecificUsers) {
      this.audienceSelection = [];
    }
  }

  next(): void {
    const s = this.step();
    if (s === 0) {
      if (!this.titleAr.trim() || !this.titleEn.trim()) {
        this.toast.show(this.i18n.t('q.surveys.wizard.toast.titlesRequired'), 'error');
        return;
      }
      if (
        this.audienceScope === SurveyAudienceScope.SpecificUsers &&
        this.audienceSelection.length === 0
      ) {
        this.toast.show(this.i18n.t('q.surveys.wizard.toast.audienceRequired'), 'error');
        return;
      }
      this.step.set(1);
      return;
    }
  }

  cancel(): void {
    if (this.surveyId) {
      void this.router.navigate(['/surveys', this.surveyId]);
    } else {
      void this.router.navigate(['/surveys']);
    }
  }

  save(): void {
    if (!this.titleAr.trim() || !this.titleEn.trim()) {
      this.toast.show(this.i18n.t('q.surveys.wizard.toast.titlesRequired'), 'error');
      return;
    }
    if (
      this.audienceScope === SurveyAudienceScope.SpecificUsers &&
      this.audienceSelection.length === 0
    ) {
      this.toast.show(this.i18n.t('q.surveys.wizard.toast.audienceRequired'), 'error');
      return;
    }
    const qs = this.normalizeForApi(this.questions);
    if (qs.length === 0) {
      this.toast.show(this.i18n.t('q.surveys.wizard.toast.questionsRequired'), 'error');
      return;
    }

    if (this.surveyId) {
      const body: UpdateSurveyRequest = {
        titleAr: this.titleAr.trim(),
        titleEn: this.titleEn.trim(),
        descriptionAr: this.descriptionAr.trim() || null,
        descriptionEn: this.descriptionEn.trim() || null,
        code: this.code.trim() || null,
        audienceScope: this.audienceScope,
        questions: qs,
        audienceMembers:
          this.audienceScope === SurveyAudienceScope.SpecificUsers ? this.mapAudienceToApi() : undefined,
        showOnPublicPortal: this.showOnPublicPortal,
        publicArticleEnabled: this.publicArticleEnabled,
        publicArticleTitleAr: this.publicArticleTitleAr.trim() || null,
        publicArticleTitleEn: this.publicArticleTitleEn.trim() || null,
        publicArticleBodyAr: this.normalizeArticleHtml(this.publicArticleBodyAr),
        publicArticleBodyEn: this.normalizeArticleHtml(this.publicArticleBodyEn),
      };
      this.saveBusy.set(true);
      this.api.update(this.surveyId, body).subscribe({
        next: () => {
          this.saveBusy.set(false);
          this.toast.show(this.i18n.t('q.detail.toast.updated'), 'success');
          void this.router.navigate(['/surveys', this.surveyId]);
        },
        error: () => {
          this.saveBusy.set(false);
          this.toast.show(this.i18n.t('q.detail.toast.updateFailed'), 'error');
        },
      });
    }
  }

  private loadSurvey(id: string): void {
    this.busy.set(true);
    forkJoin({
      survey: this.api.getById(id),
      questions: this.questionsApi.list(id).pipe(catchError(() => of([] as QuestionDto[]))),
    }).subscribe({
      next: ({ survey, questions }) => {
        this.titleAr = survey.titleAr;
        this.titleEn = survey.titleEn;
        this.descriptionAr = survey.descriptionAr ?? '';
        this.descriptionEn = survey.descriptionEn ?? '';
        this.code = survey.code ?? '';
        this.showOnPublicPortal = survey.showOnPublicPortal ?? false;
        this.publicArticleEnabled = survey.publicArticleEnabled ?? false;
        this.publicArticleTitleAr = survey.publicArticleTitleAr ?? '';
        this.publicArticleTitleEn = survey.publicArticleTitleEn ?? '';
        this.publicArticleBodyAr = survey.publicArticleBodyAr ?? '';
        this.publicArticleBodyEn = survey.publicArticleBodyEn ?? '';
        this.audienceScope = survey.audienceScope;
        this.audienceSelection = (survey.audienceMembers ?? []).map((m) => ({
          userId: m.userId,
          email: m.email,
          label: m.displayName,
          entityId: m.userId ?? m.email ?? '',
          kind: m.userId ? SurveyAudienceSubjectKind.User : SurveyAudienceSubjectKind.Partner,
        }));
        
        const rows = (questions ?? [])
          .sort((a, b) => a.displayOrder - b.displayOrder)
          .map((q) => this.stripToSimple(q));
        this.questions = rows.length > 0 ? rows : [this.emptyQuestion()];
        this.busy.set(false);
      },
      error: () => {
        this.busy.set(false);
        this.toast.show(this.i18n.t('q.detail.error'), 'error');
        void this.router.navigate(['/surveys']);
      },
    });
  }

  private stripToSimple(q: QuestionDto): CreateSurveyQuestionItem {
    return {
      type: q.type,
      titleAr: q.titleAr ?? '',
      titleEn: q.titleEn ?? '',
      isRequired: q.isRequired,
      helpTextAr: q.helpTextAr,
      helpTextEn: q.helpTextEn,
      optionsJson: q.optionsJson,
      displayOrder: q.displayOrder,
    };
  }

  private mapAudienceToApi(): SurveyAudienceMemberInputDto[] {
    return this.audienceSelection.map((p) =>
      p.userId ? { userId: p.userId, email: null } : { userId: null, email: p.email! },
    );
  }

  private normalizeForApi(rows: CreateSurveyQuestionItem[]): CreateSurveyQuestionItem[] {
    return rows
      .map((q, index) => ({
        type: q.type,
        titleAr: q.titleAr.trim(),
        titleEn: q.titleEn.trim(),
        isRequired: q.isRequired,
        helpTextAr: q.helpTextAr?.trim() || null,
        helpTextEn: q.helpTextEn?.trim() || null,
        optionsJson: q.optionsJson?.trim() || null,
        displayOrder: index + 1,
      }))
      .filter((q) => q.titleAr.length > 0 && q.titleEn.length > 0);
  }

  private normalizeArticleHtml(raw: string): string | null {
    const t = raw.trim();
    if (!t || t === '<p><br></p>' || t === '<p></p>') return null;
    return t;
  }
}
