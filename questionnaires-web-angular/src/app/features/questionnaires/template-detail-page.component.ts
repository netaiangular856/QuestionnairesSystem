import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ToastService } from '../../core/services/toast.service';
import { TemplatesApiService } from '../../services/templates-api.service';
import {
  CreateSurveyQuestionItem,
  QuestionType,
  TemplateDetailDto,
} from '../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { qLocalizedTitle } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-template-detail-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './template-detail-page.component.html',
  styleUrl: './template-detail-page.component.scss',
})
export class TemplateDetailPageComponent implements OnInit {
  private readonly api = inject(TemplatesApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);
  readonly auth = inject(AuthService);

  readonly canManage = this.auth.hasPermission(PermissionCodes.TemplateManage);

  readonly busy = signal(true);
  readonly failed = signal(false);
  readonly detail = signal<TemplateDetailDto | null>(null);
  readonly mutationBusy = signal(false);

  readonly typeLabels: { value: QuestionType; labelKey: string }[] = [
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
    if (!id) {
      void this.router.navigate(['/templates']);
      return;
    }
    this.load(id);
  }

  titleOf(d: TemplateDetailDto): string {
    return qLocalizedTitle(this.i18n.lang(), d.nameAr, d.nameEn);
  }

  questionTitle(q: CreateSurveyQuestionItem): string {
    return qLocalizedTitle(this.i18n.lang(), q.titleAr ?? '', q.titleEn ?? '');
  }

  typeLabelKey(type: QuestionType): string {
    return this.typeLabels.find((x) => x.value === type)?.labelKey ?? 'q.templates.qt.shortText';
  }

  goEdit(id: string): void {
    void this.router.navigate(['/templates', id, 'edit']);
  }

  useTemplate(d: TemplateDetailDto): void {
    if (d.isArchived) return;
    this.mutationBusy.set(true);
    this.api.use(d.id).subscribe({
      next: (r) => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.templates.toast.used'), 'success');
        void this.router.navigate(['/surveys', r.surveyId]);
      },
      error: () => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.templates.toast.useFailed'), 'error');
      },
    });
  }

  private load(id: string): void {
    this.failed.set(false);
    this.busy.set(true);
    this.api.getById(id).subscribe({
      next: (d) => {
        this.detail.set(d);
        this.busy.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.busy.set(false);
        this.toast.show(this.i18n.t('q.templates.toast.loadFailed'), 'error');
      },
    });
  }
}
