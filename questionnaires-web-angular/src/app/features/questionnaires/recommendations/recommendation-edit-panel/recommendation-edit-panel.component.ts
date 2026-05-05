import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ToastService } from '../../../../core/services/toast.service';
import { QuestionnaireLookupsApiService } from '../../../../services/questionnaire-lookups-api.service';
import { RecommendationsApiService } from '../../../../services/recommendations-api.service';
import {
  LookupItemDto,
  RecommendationPriorityTier,
  RecommendationStatus,
  UpdateRecommendationRequest,
} from '../../../../shared/models/questionnaire.models';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';
import { ApiBusinessError } from '../../../../shared/utils/api-helpers';
import { openDatetimeLocalPicker } from '../../../../shared/utils/open-datetime-local-picker';
import { DropdownModule } from 'primeng/dropdown';

function tierFromPriority(p: number): RecommendationPriorityTier {
  if (p >= 7) return 'high';
  if (p >= 4) return 'medium';
  return 'low';
}

function tierToPriority(t: RecommendationPriorityTier): number {
  switch (t) {
    case 'high':
      return 8;
    case 'low':
      return 3;
    default:
      return 5;
  }
}

function isoToDatetimeLocalValue(iso: string | null | undefined): string {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '';
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

@Component({
  selector: 'app-recommendation-edit-panel',
  standalone: true,
  imports: [FormsModule, TranslatePipe, DropdownModule],
  templateUrl: './recommendation-edit-panel.component.html',
  styleUrl: './recommendation-edit-panel.component.scss',
})
export class RecommendationEditPanelComponent implements OnChanges {
  private readonly api = inject(RecommendationsApiService);
  private readonly lookupsApi = inject(QuestionnaireLookupsApiService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  @Input({ required: true }) recommendationId!: string;

  @Output() readonly saved = new EventEmitter<void>();
  @Output() readonly cancelled = new EventEmitter<void>();

  readonly RecommendationStatus = RecommendationStatus;

  readonly loadBusy = signal(true);
  readonly loadFailed = signal(false);
  readonly submitBusy = signal(false);
  readonly surveys = signal<LookupItemDto[]>([]);
  readonly users = signal<LookupItemDto[]>([]);

  selectedSurveyId: string | null = null;
  assigneePick = '';
  titleAr = '';
  titleEn = '';
  descriptionAr = '';
  descriptionEn = '';
  priorityTier: RecommendationPriorityTier = 'medium';
  status: RecommendationStatus = RecommendationStatus.Draft;
  dueDateLocal = '';

  surveyErr = '';
  titleArErr = '';
  titleEnErr = '';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['recommendationId'] && this.recommendationId) {
      this.loadRecommendation();
    }
  }

  private loadRecommendation(): void {
    this.loadBusy.set(true);
    this.loadFailed.set(false);
    forkJoin({
      rec: this.api.getById(this.recommendationId),
      surveys: this.lookupsApi.getSurveys('', 500).pipe(catchError(() => of([] as LookupItemDto[]))),
      users: this.lookupsApi.getUsers('', 500).pipe(catchError(() => of([] as LookupItemDto[]))),
    }).subscribe({
      next: ({ rec, surveys, users }) => {
        let userList = [...users];
        if (
          rec.assignedToUserId &&
          !userList.some((u) => u.id === rec.assignedToUserId) &&
          rec.assignedToDisplayName
        ) {
          userList = [
            { id: rec.assignedToUserId, name: rec.assignedToDisplayName, email: null },
            ...userList,
          ];
        }
        this.surveys.set(surveys);
        this.users.set(userList);
        this.selectedSurveyId = rec.surveyId;
        this.assigneePick = rec.assignedToUserId ?? '';
        this.titleAr = rec.titleAr;
        this.titleEn = rec.titleEn;
        this.descriptionAr = rec.descriptionAr ?? '';
        this.descriptionEn = rec.descriptionEn ?? '';
        this.priorityTier = tierFromPriority(rec.priority);
        this.status = rec.status;
        this.dueDateLocal = isoToDatetimeLocalValue(rec.dueDateUtc);
        this.loadBusy.set(false);
      },
      error: () => {
        this.loadFailed.set(true);
        this.loadBusy.set(false);
      },
    });
  }

  surveyLabel(s: LookupItemDto): string {
    return s.name?.trim() || '—';
  }

  userLabel(u: LookupItemDto): string {
    const n = u.name?.trim() || '—';
    const e = u.email?.trim();
    return e ? `${n} (${e})` : n;
  }

  assigneeDropdownOptions(): { value: string; label: string }[] {
    return [
      { value: '', label: this.i18n.t('q.rec.create.assigneeNone') },
      ...this.users().map((u) => ({ value: u.id, label: this.userLabel(u) })),
    ];
  }

  clearFieldErrors(): void {
    this.surveyErr = '';
    this.titleArErr = '';
    this.titleEnErr = '';
  }

  submit(): void {
    this.clearFieldErrors();
    const tAr = this.titleAr.trim();
    const tEn = this.titleEn.trim();
    let valid = true;
    if (!this.selectedSurveyId) {
      this.surveyErr = this.i18n.t('q.rec.validation.survey');
      valid = false;
    }
    if (!tAr) {
      this.titleArErr = this.i18n.t('q.rec.validation.titleAr');
      valid = false;
    }
    if (!tEn) {
      this.titleEnErr = this.i18n.t('q.rec.validation.titleEn');
      valid = false;
    }
    if (!valid) {
      this.toast.show(this.i18n.t('q.rec.validation.summary'), 'error');
      return;
    }

    let dueDateUtc: string | null = null;
    if (this.dueDateLocal?.trim()) {
      const d = new Date(this.dueDateLocal);
      if (!Number.isNaN(d.getTime())) dueDateUtc = d.toISOString();
    }

    const body: UpdateRecommendationRequest = {
      titleAr: tAr,
      titleEn: tEn,
      descriptionAr: this.descriptionAr.trim() || null,
      descriptionEn: this.descriptionEn.trim() || null,
      priority: tierToPriority(this.priorityTier),
      status: this.status,
      assignedToUserId: this.assigneePick || null,
      dueDateUtc,
      surveyId: this.selectedSurveyId,
    };

    this.submitBusy.set(true);
    this.api.update(this.recommendationId, body).subscribe({
      next: () => {
        this.submitBusy.set(false);
        this.toast.show(this.i18n.t('q.rec.edit.success'), 'success');
        this.saved.emit();
      },
      error: (err: unknown) => {
        this.submitBusy.set(false);
        const msg =
          err instanceof ApiBusinessError && err.errors.length > 0
            ? err.errors[0]
            : this.i18n.t('q.rec.edit.error');
        this.toast.show(msg, 'error');
      },
    });
  }

  cancel(): void {
    this.cancelled.emit();
  }

  openDuePicker(input: HTMLInputElement): void {
    openDatetimeLocalPicker(input);
  }
}
