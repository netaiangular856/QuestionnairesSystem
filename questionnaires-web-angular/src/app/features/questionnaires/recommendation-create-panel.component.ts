import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  OnInit,
  Output,
  SimpleChanges,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ToastService } from '../../core/services/toast.service';
import { QuestionnaireLookupsApiService } from '../../services/questionnaire-lookups-api.service';
import { RecommendationsApiService } from '../../services/recommendations-api.service';
import {
  CreateRecommendationRequest,
  LookupItemDto,
  type RecommendationPriorityTier,
} from '../../shared/models/questionnaire.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { ApiBusinessError } from '../../shared/utils/api-helpers';
import { openDatetimeLocalPicker } from '../../shared/utils/open-datetime-local-picker';
import { DropdownModule } from 'primeng/dropdown';

@Component({
  selector: 'app-recommendation-create-panel',
  standalone: true,
  imports: [FormsModule, TranslatePipe, DropdownModule],
  templateUrl: './recommendation-create-panel.component.html',
  styleUrl: './recommendation-create-panel.component.scss',
})
export class RecommendationCreatePanelComponent implements OnInit, OnChanges {
  private readonly api = inject(RecommendationsApiService);
  private readonly lookupsApi = inject(QuestionnaireLookupsApiService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  /** When set, the recommendation is always created for this survey (no dropdown). */
  @Input() lockedSurveyId: string | null = null;
  /** Shown when `lockedSurveyId` is set (e.g. current survey title). */
  @Input() lockedSurveyLabel: string | null = null;
  /** Pre-select a survey in the dropdown (e.g. from `?surveyId=` on /recommendations). */
  @Input() initialSurveyId: string | null = null;

  @Output() readonly created = new EventEmitter<void>();

  readonly surveys = signal<LookupItemDto[]>([]);
  readonly surveysFailed = signal(false);
  readonly surveysBusy = signal(false);
  readonly users = signal<LookupItemDto[]>([]);
  readonly usersBusy = signal(false);
  readonly submitBusy = signal(false);

  selectedSurveyId: string | null = null;
  /** Assignee user id; empty string = none (PrimeNG dropdown option value). */
  assigneePick = '';
  titleAr = '';
  titleEn = '';
  descriptionAr = '';
  descriptionEn = '';
  priorityTier: RecommendationPriorityTier = 'medium';
  /** Value for `<input type="datetime-local">` in local time. */
  dueDateLocal = '';

  /** Inline validation (cleared on successful submit or when user edits). */
  surveyErr = '';
  titleArErr = '';
  titleEnErr = '';

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['initialSurveyId'] || this.lockedSurveyId) return;
    if (this.surveysBusy() || !this.surveys().length) return;
    this.applyInitialSurveySelection();
  }

  ngOnInit(): void {
    const users$ = this.lookupsApi.getUsers('', 500).pipe(catchError(() => of([] as LookupItemDto[])));

    if (this.lockedSurveyId) {
      this.selectedSurveyId = this.lockedSurveyId;
      this.usersBusy.set(true);
      users$.subscribe({
        next: (list) => {
          this.users.set(list);
          this.usersBusy.set(false);
        },
      });
      return;
    }

    this.surveysBusy.set(true);
    this.usersBusy.set(true);
    forkJoin({
      surveys: this.lookupsApi.getSurveys('', 500).pipe(
        catchError(() => {
          this.surveysFailed.set(true);
          return of([] as LookupItemDto[]);
        }),
      ),
      users: users$,
    }).subscribe({
      next: ({ surveys, users }) => {
        this.surveys.set(surveys);
        this.users.set(users);
        this.surveysBusy.set(false);
        this.usersBusy.set(false);
        this.applyInitialSurveySelection();
      },
      error: () => {
        this.surveysFailed.set(true);
        this.surveysBusy.set(false);
        this.usersBusy.set(false);
      },
    });
  }

  private applyInitialSurveySelection(): void {
    const want = this.initialSurveyId;
    if (!want) return;
    const exists = this.surveys().some((s) => s.id === want);
    this.selectedSurveyId = exists ? want : null;
  }

  surveyLabel(s: LookupItemDto): string {
    return s.name?.trim() || '—';
  }

  userLabel(u: LookupItemDto): string {
    const n = u.name?.trim() || '—';
    const e = u.email?.trim();
    return e ? `${n} (${e})` : n;
  }

  /** Options for `p-dropdown` (filter is inside the overlay). */
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

  openDuePicker(input: HTMLInputElement): void {
    openDatetimeLocalPicker(input);
  }

  private tierToPriority(): number {
    switch (this.priorityTier) {
      case 'low':
        return 3;
      case 'high':
        return 8;
      default:
        return 5;
    }
  }

  submit(): void {
    this.clearFieldErrors();
    const surveyId = this.lockedSurveyId ?? this.selectedSurveyId;
    const tAr = this.titleAr.trim();
    const tEn = this.titleEn.trim();
    let valid = true;
    if (!surveyId) {
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

    const body: CreateRecommendationRequest = {
      surveyId,
      titleAr: tAr,
      titleEn: tEn,
      descriptionAr: this.descriptionAr.trim() || null,
      descriptionEn: this.descriptionEn.trim() || null,
      priority: this.tierToPriority(),
      assignedToUserId: this.assigneePick || null,
      dueDateUtc,
    };

    this.submitBusy.set(true);
    this.api.create(body).subscribe({
      next: () => {
        this.submitBusy.set(false);
        this.toast.show(this.i18n.t('q.rec.create.success'), 'success');
        this.titleAr = '';
        this.titleEn = '';
        this.descriptionAr = '';
        this.descriptionEn = '';
        this.priorityTier = 'medium';
        this.dueDateLocal = '';
        this.assigneePick = '';
        this.clearFieldErrors();
        this.created.emit();
      },
      error: (err: unknown) => {
        this.submitBusy.set(false);
        const msg =
          err instanceof ApiBusinessError && err.errors.length > 0
            ? err.errors[0]
            : this.i18n.t('q.rec.create.error');
        this.toast.show(msg, 'error');
      },
    });
  }
}
