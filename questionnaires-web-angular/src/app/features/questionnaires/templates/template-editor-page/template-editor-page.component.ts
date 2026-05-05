import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { AiApiService } from '../../../../services/ai-api.service';
import { TemplatesApiService } from '../../../../services/templates-api.service';
import {
  CreateSurveyQuestionItem,
  CreateTemplateRequest,
  QuestionType,
  UpdateTemplateRequest,
} from '../../../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../../../shared/models/permission-codes';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';
import { ApiBusinessError } from '../../../../shared/utils/api-helpers';
import {
  htmlFragmentToPlainText,
  resolveBilingualTranslateSource,
} from '../../../../shared/utils/bilingual-ai-translate';

@Component({
  selector: 'app-template-editor-page',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe],
  templateUrl: './template-editor-page.component.html',
  styleUrl: './template-editor-page.component.scss',
})
export class TemplateEditorPageComponent implements OnInit {
  private readonly api = inject(TemplatesApiService);
  private readonly aiApi = inject(AiApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  readonly busy = signal(false);
  readonly saveBusy = signal(false);
  readonly translateBusy = signal(false);
  readonly isEdit = signal(false);
  private templateId: string | null = null;

  nameAr = '';
  nameEn = '';
  descriptionAr = '';
  descriptionEn = '';
  questions: CreateSurveyQuestionItem[] = [this.emptyQuestion()];

  readonly QuestionType = QuestionType;

  readonly canAiTranslate = () => this.auth.hasPermission(PermissionCodes.TemplateManage);

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
    const id = this.route.snapshot.paramMap.get('templateId');
    if (id) {
      this.templateId = id;
      this.isEdit.set(true);
      this.loadTemplate(id);
    } else {
      this.isEdit.set(false);
    }
  }

  pageTitleKey(): string {
    return this.isEdit() ? 'q.templates.editor.titleEdit' : 'q.templates.editor.titleNew';
  }

  badgeKey(): string {
    return this.isEdit() ? 'q.templates.editor.badgeEdit' : 'q.templates.editor.badgeNew';
  }

  get hintsAriaLabel(): string {
    return this.i18n.t('q.templates.editor.hintsAria');
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

  translateNamesPair(): void {
    const r = resolveBilingualTranslateSource(this.nameAr, this.nameEn, this.i18n.lang());
    if (!r) {
      this.toast.show(this.i18n.t('q.ai.translateEmpty'), 'error');
      return;
    }
    this.aiTranslate(
      r.from,
      r.source,
      (out) => {
        if (r.from === 'ar') this.nameEn = out;
        else this.nameAr = out;
      },
      { plainOutput: true }
    );
  }

  translateDescPair(): void {
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

  translateQuestionTitlesPair(q: CreateSurveyQuestionItem): void {
    const r = resolveBilingualTranslateSource(q.titleAr ?? '', q.titleEn ?? '', this.i18n.lang());
    if (!r) {
      this.toast.show(this.i18n.t('q.ai.translateEmpty'), 'error');
      return;
    }
    this.aiTranslate(
      r.from,
      r.source,
      (out) => {
        if (r.from === 'ar') q.titleEn = out;
        else q.titleAr = out;
      },
      { plainOutput: true }
    );
  }

  addQuestion(): void {
    this.questions = [...this.questions, this.emptyQuestion()];
  }

  removeQuestion(index: number): void {
    if (this.questions.length <= 1) return;
    this.questions = this.questions.filter((_, i) => i !== index);
  }

  cancel(): void {
    void this.router.navigate(['/templates']);
  }

  save(): void {
    if (!this.nameAr.trim() || !this.nameEn.trim()) {
      this.toast.show(this.i18n.t('q.templates.toast.namesRequired'), 'error');
      return;
    }
    const qs = this.normalizeForApi(this.questions);
    if (qs.length === 0) {
      this.toast.show(this.i18n.t('q.templates.toast.questionsRequired'), 'error');
      return;
    }

    if (this.templateId) {
      const body: UpdateTemplateRequest = {
        nameAr: this.nameAr.trim(),
        nameEn: this.nameEn.trim(),
        descriptionAr: this.descriptionAr.trim() || null,
        descriptionEn: this.descriptionEn.trim() || null,
        questions: qs,
      };
      this.saveBusy.set(true);
      this.api.update(this.templateId, body).subscribe({
        next: () => {
          this.saveBusy.set(false);
          this.toast.show(this.i18n.t('q.templates.toast.updated'), 'success');
          void this.router.navigate(['/templates']);
        },
        error: () => {
          this.saveBusy.set(false);
          this.toast.show(this.i18n.t('q.templates.toast.updateFailed'), 'error');
        },
      });
    } else {
      const body: CreateTemplateRequest = {
        nameAr: this.nameAr.trim(),
        nameEn: this.nameEn.trim(),
        descriptionAr: this.descriptionAr.trim() || null,
        descriptionEn: this.descriptionEn.trim() || null,
        questions: qs,
      };
      this.saveBusy.set(true);
      this.api.create(body).subscribe({
        next: () => {
          this.saveBusy.set(false);
          this.toast.show(this.i18n.t('q.templates.toast.created'), 'success');
          void this.router.navigate(['/templates']);
        },
        error: () => {
          this.saveBusy.set(false);
          this.toast.show(this.i18n.t('q.templates.toast.createFailed'), 'error');
        },
      });
    }
  }

  private loadTemplate(id: string): void {
    this.busy.set(true);
    this.api.getById(id).subscribe({
      next: (d) => {
        this.nameAr = d.nameAr;
        this.nameEn = d.nameEn;
        this.descriptionAr = d.descriptionAr ?? '';
        this.descriptionEn = d.descriptionEn ?? '';
        const rows = (d.questions ?? []).map((q) => this.stripToSimple(q));
        this.questions = rows.length > 0 ? rows : [this.emptyQuestion()];
        this.busy.set(false);
      },
      error: () => {
        this.busy.set(false);
        this.toast.show(this.i18n.t('q.templates.toast.loadFailed'), 'error');
        void this.router.navigate(['/templates']);
      },
    });
  }

  /** يعرض للمستخدم نوع السؤال والنص فقط؛ يُزال أي حقل متقدم عند الحفظ. */
  private stripToSimple(q: CreateSurveyQuestionItem): CreateSurveyQuestionItem {
    return {
      type: q.type,
      titleAr: q.titleAr ?? '',
      titleEn: q.titleEn ?? '',
      isRequired: false,
      helpTextAr: null,
      helpTextEn: null,
      optionsJson: null,
      displayOrder: null,
    };
  }

  private normalizeForApi(rows: CreateSurveyQuestionItem[]): CreateSurveyQuestionItem[] {
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
}
