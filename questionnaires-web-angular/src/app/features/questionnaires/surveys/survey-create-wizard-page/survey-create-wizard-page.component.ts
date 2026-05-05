import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { AiApiService } from '../../../../services/ai-api.service';
import { SurveysApiService } from '../../../../services/surveys-api.service';
import { TemplatesApiService } from '../../../../services/templates-api.service';
import {
  CreateSurveyQuestionItem,
  CreateSurveyRequest,
  QuestionType,
  SurveyAudienceMemberInputDto,
  SurveyAudiencePickItem,
  SurveyAudienceScope,
  TemplateDetailDto,
  TemplateListItemDto,
} from '../../../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../../../shared/models/permission-codes';
import { qLocalizedTitle } from '../../../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';
import { ApiBusinessError } from '../../../../shared/utils/api-helpers';
import {
  htmlFragmentToPlainText,
  resolveBilingualTranslateSource,
} from '../../../../shared/utils/bilingual-ai-translate';
import { SurveyAudiencePickerComponent } from '../../../../shared/questionnaires/survey-audience-picker/survey-audience-picker.component';
import { PublicArticleEditorComponent } from '../../../../shared/questionnaires/public-article-editor/public-article-editor.component';

type SourceMode = 'blank' | 'template';

@Component({
  selector: 'app-survey-create-wizard-page',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe, SurveyAudiencePickerComponent, PublicArticleEditorComponent],
  templateUrl: './survey-create-wizard-page.component.html',
  styleUrl: './survey-create-wizard-page.component.scss',
})
export class SurveyCreateWizardPageComponent implements OnInit {
  private readonly api = inject(SurveysApiService);
  private readonly templatesApi = inject(TemplatesApiService);
  private readonly aiApi = inject(AiApiService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  readonly translateBusy = signal(false);

  readonly canAiTranslate = () =>
    this.auth.hasPermission(PermissionCodes.SurveyManage);

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
  showOnPublicPortal = false;
  publicArticleEnabled = false;
  publicArticleTitleAr = '';
  publicArticleTitleEn = '';
  publicArticleBodyAr = '';
  publicArticleBodyEn = '';
  audienceScope = SurveyAudienceScope.AllOrganizationMembers;
  /** When audience is SpecificUsers — chips from unified lookup. */
  audienceSelection: SurveyAudiencePickItem[] = [];
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

  /** Visible calendar button — native picker glyph is often invisible with themed inputs / RTL. */
  openDateTimePicker(input: HTMLInputElement, ev: Event): void {
    ev.preventDefault();
    ev.stopPropagation();
    const el = input as HTMLInputElement & { showPicker?: () => void };
    if (typeof el.showPicker === 'function') {
      try {
        void el.showPicker();
        return;
      } catch {
        /* Unsupported context */
      }
    }
    el.focus();
    el.click();
  }

  /**
   * Fills the opposite language field using AI. Works for plain text or HTML fragments from rich editors.
   */
  aiTranslate(
    from: 'ar' | 'en',
    sourceHtml: string,
    setTarget: (value: string) => void,
    options?: { plainOutput?: boolean }
  ): void {
    if (!this.canAiTranslate()) return;
    const src = sourceHtml?.trim();
    if (!src) {
      this.toast.show(this.i18n.t('q.ai.translateEmpty'), 'error');
      return;
    }
    const targetLang = from === 'ar' ? 'en' : 'ar';
    this.translateBusy.set(true);
    this.aiApi.translateRichText({ html: src, sourceLang: from, targetLang }).subscribe({
      next: (r) => {
        let out = r.html ?? '';
        if (options?.plainOutput) {
          out = htmlFragmentToPlainText(out);
        }
        setTarget(out);
        this.translateBusy.set(false);
        this.toast.show(this.i18n.t('q.ai.translateDone'), 'success');
      },
      error: (err: unknown) => {
        this.translateBusy.set(false);
        const msg =
          err instanceof ApiBusinessError && err.errors.length > 0
            ? err.errors[0]
            : this.i18n.t('q.ai.suggestFailed');
        this.toast.show(msg, 'error');
      },
    });
  }

  translateTitlesPair(): void {
    const r = resolveBilingualTranslateSource(this.titleAr, this.titleEn, this.i18n.lang());
    if (!r) {
      this.toast.show(this.i18n.t('q.ai.translateEmpty'), 'error');
      return;
    }
    this.aiTranslate(
      r.from,
      r.source,
      (out) => {
        if (r.from === 'ar') this.titleEn = out;
        else this.titleAr = out;
      },
      { plainOutput: true }
    );
  }

  translateDescriptionsPair(): void {
    const r = resolveBilingualTranslateSource(this.descriptionAr, this.descriptionEn, this.i18n.lang());
    if (!r) {
      this.toast.show(this.i18n.t('q.ai.translateEmpty'), 'error');
      return;
    }
    this.aiTranslate(
      r.from,
      r.source,
      (out) => {
        if (r.from === 'ar') this.descriptionEn = out;
        else this.descriptionAr = out;
      },
      { plainOutput: true }
    );
  }

  translatePublicTitlesPair(): void {
    const r = resolveBilingualTranslateSource(
      this.publicArticleTitleAr,
      this.publicArticleTitleEn,
      this.i18n.lang()
    );
    if (!r) {
      this.toast.show(this.i18n.t('q.ai.translateEmpty'), 'error');
      return;
    }
    this.aiTranslate(
      r.from,
      r.source,
      (out) => {
        if (r.from === 'ar') this.publicArticleTitleEn = out;
        else this.publicArticleTitleAr = out;
      },
      { plainOutput: true }
    );
  }

  /** Rich HTML — keep markup from API for the article editors. */
  translatePublicBodiesPair(arEditor?: PublicArticleEditorComponent, enEditor?: PublicArticleEditorComponent): void {
    arEditor?.flushValueFromEditor();
    enEditor?.flushValueFromEditor();
    const r = resolveBilingualTranslateSource(
      this.publicArticleBodyAr,
      this.publicArticleBodyEn,
      this.i18n.lang()
    );
    if (!r) {
      this.toast.show(this.i18n.t('q.ai.translateEmpty'), 'error');
      return;
    }
    this.aiTranslate(r.from, r.source, (out) => {
      if (r.from === 'ar') this.publicArticleBodyEn = out;
      else this.publicArticleBodyAr = out;
    });
  }

  prev(): void {
    const s = this.step();
    if (s <= 0) {
      void this.router.navigate(['/surveys']);
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

  onQuestionTypeChange(q: CreateSurveyQuestionItem): void {
    if (q.type === QuestionType.SingleChoice || q.type === QuestionType.MultipleChoice) {
      if (!q.choiceOptions?.length) {
        q.choiceOptions = [
          { labelAr: '', labelEn: '' },
          { labelAr: '', labelEn: '' },
        ];
      }
    } else {
      q.choiceOptions = undefined;
    }
  }

  addChoiceOption(q: CreateSurveyQuestionItem): void {
    q.choiceOptions = [...(q.choiceOptions ?? []), { labelAr: '', labelEn: '' }];
  }

  removeChoiceOption(q: CreateSurveyQuestionItem, index: number): void {
    const rows = q.choiceOptions ?? [];
    if (rows.length <= 2) return;
    q.choiceOptions = rows.filter((_, i) => i !== index);
  }

  submit(): void {
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
    if (this.sourceMode === 'blank') {
      if (!this.validateChoiceQuestions(this.questions)) return;
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
    if (this.extraQuestions.length > 0 && !this.validateChoiceQuestions(this.extraQuestions)) return;
    const extras = this.normalizeList(this.extraQuestions);
    this.postCreate({
      ...this.buildBaseBody(),
      templateId: tid,
      questions: extras.length > 0 ? extras : null,
    });
  }

  private mapAudienceToApi(): SurveyAudienceMemberInputDto[] {
    return this.audienceSelection.map((p) =>
      p.userId
        ? { userId: p.userId, email: null }
        : { userId: null, email: p.email! },
    );
  }

  private buildBaseBody(): Pick<
    CreateSurveyRequest,
    | 'titleAr'
    | 'titleEn'
    | 'descriptionAr'
    | 'descriptionEn'
    | 'code'
    | 'audienceScope'
    | 'opensAtUtc'
    | 'closesAtUtc'
    | 'audienceMembers'
    | 'showOnPublicPortal'
    | 'publicArticleEnabled'
    | 'publicArticleTitleAr'
    | 'publicArticleTitleEn'
    | 'publicArticleBodyAr'
    | 'publicArticleBodyEn'
  > {
    const base: Pick<
      CreateSurveyRequest,
      | 'titleAr'
      | 'titleEn'
      | 'descriptionAr'
      | 'descriptionEn'
      | 'code'
      | 'audienceScope'
      | 'opensAtUtc'
      | 'closesAtUtc'
      | 'audienceMembers'
      | 'showOnPublicPortal'
      | 'publicArticleEnabled'
      | 'publicArticleTitleAr'
      | 'publicArticleTitleEn'
      | 'publicArticleBodyAr'
      | 'publicArticleBodyEn'
    > = {
      titleAr: this.titleAr.trim(),
      titleEn: this.titleEn.trim(),
      descriptionAr: this.descriptionAr.trim() || null,
      descriptionEn: this.descriptionEn.trim() || null,
      code: this.code.trim() || null,
      audienceScope: this.audienceScope,
      opensAtUtc: this.toIsoOrNull(this.opensAtLocal),
      closesAtUtc: this.toIsoOrNull(this.closesAtLocal),
      audienceMembers:
        this.audienceScope === SurveyAudienceScope.SpecificUsers ? this.mapAudienceToApi() : null,
      showOnPublicPortal: this.showOnPublicPortal,
      publicArticleEnabled: this.publicArticleEnabled,
      publicArticleTitleAr: this.publicArticleTitleAr.trim() || null,
      publicArticleTitleEn: this.publicArticleTitleEn.trim() || null,
      publicArticleBodyAr: this.normalizeArticleHtml(this.publicArticleBodyAr),
      publicArticleBodyEn: this.normalizeArticleHtml(this.publicArticleBodyEn),
    };
    return base;
  }

  /** Treat empty Quill output as null when saving. */
  private normalizeArticleHtml(raw: string): string | null {
    const t = raw.trim();
    if (!t || t === '<p><br></p>' || t === '<p></p>') return null;
    return t;
  }

  private toIsoOrNull(local: string): string | null {
    if (!local?.trim()) return null;
    const d = new Date(local);
    if (Number.isNaN(d.getTime())) return null;
    return d.toISOString();
  }

  private validateChoiceQuestions(rows: CreateSurveyQuestionItem[]): boolean {
    for (const q of rows) {
      if (q.type !== QuestionType.SingleChoice && q.type !== QuestionType.MultipleChoice) continue;
      const json = this.serializeChoiceOptions(q);
      if (!json) {
        this.toast.show(this.i18n.t('q.surveys.wizard.toast.choiceOptionsRequired'), 'error');
        return false;
      }
    }
    return true;
  }

  /** Builds JSON array expected by fill page: `{ value, labelAr, labelEn }[]`. */
  private serializeChoiceOptions(q: CreateSurveyQuestionItem): string | null {
    if (q.type !== QuestionType.SingleChoice && q.type !== QuestionType.MultipleChoice) return null;
    const rows = q.choiceOptions ?? [];
    const payload = rows
      .map((r, i) => ({
        value: `opt_${i + 1}`,
        labelAr: (r.labelAr ?? '').trim(),
        labelEn: (r.labelEn ?? '').trim(),
      }))
      .filter((r) => r.labelAr.length > 0 && r.labelEn.length > 0);
    return payload.length >= 2 ? JSON.stringify(payload) : null;
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
        optionsJson: this.serializeChoiceOptions(q),
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
