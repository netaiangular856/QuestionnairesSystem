import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ToastService } from '../../core/services/toast.service';
import { SurveysApiService } from '../../services/surveys-api.service';
import { TemplatesApiService } from '../../services/templates-api.service';
import {
  CreateSurveyQuestionItem,
  CreateSurveyRequest,
  QuestionType,
  SurveyAudienceScope,
  TemplateDetailDto,
  TemplateListItemDto,
} from '../../shared/models/questionnaire.models';
import { qLocalizedTitle } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

type SourceMode = 'blank' | 'template';

@Component({
  selector: 'app-survey-create-wizard-page',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe],
  templateUrl: './survey-create-wizard-page.component.html',
  styleUrl: './survey-create-wizard-page.component.scss',
})
export class SurveyCreateWizardPageComponent implements OnInit {
  private readonly api = inject(SurveysApiService);
  private readonly templatesApi = inject(TemplatesApiService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  readonly step = signal(0);
  readonly saveBusy = signal(false);
  readonly templateLoadBusy = signal(false);

  readonly templates = signal<TemplateListItemDto[]>([]);
  readonly templateDetail = signal<TemplateDetailDto | null>(null);

  readonly SurveyAudienceScope = SurveyAudienceScope;
  readonly QuestionType = QuestionType;

  sourceMode: SourceMode = 'blank';
  templateId: string | null = null;

  titleAr = '';
  titleEn = '';
  descriptionAr = '';
  descriptionEn = '';
  code = '';
  audienceScope = SurveyAudienceScope.AllOrganizationMembers;
  opensAtLocal = '';
  closesAtLocal = '';

  questions: CreateSurveyQuestionItem[] = [this.emptyQuestion()];
  extraQuestions: CreateSurveyQuestionItem[] = [];

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
    this.templatesApi.list(false).subscribe({
      next: (list) => this.templates.set(list ?? []),
      error: () => this.templates.set([]),
    });
  }

  stepLabelKey(i: number): string {
    const keys = ['q.surveys.wizard.stepMeta', 'q.surveys.wizard.stepSource', 'q.surveys.wizard.stepQuestions'] as const;
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

  setSourceMode(mode: SourceMode): void {
    this.sourceMode = mode;
    if (mode === 'blank') {
      this.templateId = null;
      this.templateDetail.set(null);
    }
  }

  prev(): void {
    const s = this.step();
    if (s <= 0) {
      void this.router.navigate(['/surveys']);
      return;
    }
    this.step.set(s - 1);
  }

  next(): void {
    const s = this.step();
    if (s === 0) {
      if (!this.titleAr.trim() || !this.titleEn.trim()) {
        this.toast.show(this.i18n.t('q.surveys.wizard.toast.titlesRequired'), 'error');
        return;
      }
      this.step.set(1);
      return;
    }
    if (s === 1) {
      if (this.sourceMode === 'template') {
        if (!this.templateId?.trim()) {
          this.toast.show(this.i18n.t('q.surveys.wizard.toast.pickTemplate'), 'error');
          return;
        }
        this.loadTemplateForQuestions();
      } else {
        this.templateDetail.set(null);
        if (this.questions.length === 0) this.questions = [this.emptyQuestion()];
        this.step.set(2);
      }
      return;
    }
  }

  private loadTemplateForQuestions(): void {
    const id = this.templateId?.trim();
    if (!id) return;
    this.templateLoadBusy.set(true);
    this.templatesApi.getById(id).subscribe({
      next: (d) => {
        this.templateDetail.set(d);
        this.templateLoadBusy.set(false);
        this.extraQuestions = [];
        this.step.set(2);
      },
      error: () => {
        this.templateLoadBusy.set(false);
        this.toast.show(this.i18n.t('q.templates.toast.loadFailed'), 'error');
      },
    });
  }

  addQuestion(): void {
    this.questions = [...this.questions, this.emptyQuestion()];
  }

  removeQuestion(i: number): void {
    if (this.questions.length <= 1) return;
    this.questions = this.questions.filter((_, j) => j !== i);
  }

  addExtra(): void {
    this.extraQuestions = [...this.extraQuestions, this.emptyQuestion()];
  }

  removeExtra(i: number): void {
    this.extraQuestions = this.extraQuestions.filter((_, j) => j !== i);
  }

  templateTitle(t: TemplateListItemDto): string {
    return qLocalizedTitle(this.i18n.lang(), t.nameAr, t.nameEn);
  }

  questionTitleFromItem(q: CreateSurveyQuestionItem): string {
    return qLocalizedTitle(this.i18n.lang(), q.titleAr ?? '', q.titleEn ?? '');
  }

  typeLabelKey(t: number): string {
    return this.questionTypes.find((x) => x.value === t)?.labelKey ?? 'q.templates.qt.shortText';
  }

  submit(): void {
    if (!this.titleAr.trim() || !this.titleEn.trim()) {
      this.toast.show(this.i18n.t('q.surveys.wizard.toast.titlesRequired'), 'error');
      return;
    }
    if (this.sourceMode === 'blank') {
      const qs = this.normalizeList(this.questions);
      if (qs.length === 0) {
        this.toast.show(this.i18n.t('q.surveys.wizard.toast.questionsRequired'), 'error');
        return;
      }
      this.postCreate({
        ...this.buildBaseBody(),
        templateId: null,
        questions: qs,
      });
      return;
    }
    const tid = this.templateId?.trim();
    if (!tid) {
      this.toast.show(this.i18n.t('q.surveys.wizard.toast.pickTemplate'), 'error');
      return;
    }
    const extras = this.normalizeList(this.extraQuestions);
    this.postCreate({
      ...this.buildBaseBody(),
      templateId: tid,
      questions: extras.length > 0 ? extras : null,
    });
  }

  private buildBaseBody(): Pick<
    CreateSurveyRequest,
    'titleAr' | 'titleEn' | 'descriptionAr' | 'descriptionEn' | 'code' | 'audienceScope' | 'opensAtUtc' | 'closesAtUtc'
  > {
    return {
      titleAr: this.titleAr.trim(),
      titleEn: this.titleEn.trim(),
      descriptionAr: this.descriptionAr.trim() || null,
      descriptionEn: this.descriptionEn.trim() || null,
      code: this.code.trim() || null,
      audienceScope: this.audienceScope,
      opensAtUtc: this.toIsoOrNull(this.opensAtLocal),
      closesAtUtc: this.toIsoOrNull(this.closesAtLocal),
    };
  }

  private toIsoOrNull(local: string): string | null {
    if (!local?.trim()) return null;
    const d = new Date(local);
    if (Number.isNaN(d.getTime())) return null;
    return d.toISOString();
  }

  private normalizeList(rows: CreateSurveyQuestionItem[]): CreateSurveyQuestionItem[] {
    return rows
      .map((q) => ({
        type: q.type,
        titleAr: q.titleAr.trim(),
        titleEn: q.titleEn.trim(),
        isRequired: false,
        helpTextAr: null as string | null,
        helpTextEn: null as string | null,
        optionsJson: null as string | null,
        displayOrder: null as number | null,
      }))
      .filter((q) => q.titleAr.length > 0 && q.titleEn.length > 0);
  }

  private postCreate(body: CreateSurveyRequest): void {
    this.saveBusy.set(true);
    this.api.create(body).subscribe({
      next: (s) => {
        this.saveBusy.set(false);
        this.toast.show(this.i18n.t('q.surveys.toast.created'), 'success');
        void this.router.navigate(['/surveys', s.id]);
      },
      error: () => {
        this.saveBusy.set(false);
        this.toast.show(this.i18n.t('q.surveys.toast.createFailed'), 'error');
      },
    });
  }
}
