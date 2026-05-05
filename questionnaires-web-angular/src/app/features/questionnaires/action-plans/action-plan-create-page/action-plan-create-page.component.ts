import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { AiApiService } from '../../../../services/ai-api.service';
import { ActionPlansApiService } from '../../../../services/action-plans-api.service';
import { QuestionnaireLookupsApiService } from '../../../../services/questionnaire-lookups-api.service';
import { LookupItemDto } from '../../../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../../../shared/models/permission-codes';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';
import { ApiBusinessError } from '../../../../shared/utils/api-helpers';
import { openDatePicker as openNativeDatePicker } from '../../../../shared/utils/open-datetime-local-picker';

@Component({
  selector: 'app-action-plan-create-page',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe],
  templateUrl: './action-plan-create-page.component.html',
  styleUrl: './action-plan-create-page.component.scss',
})
export class ActionPlanCreatePageComponent implements OnInit {
  readonly openDatePicker = openNativeDatePicker;

  private readonly api = inject(ActionPlansApiService);
  private readonly aiApi = inject(AiApiService);
  private readonly lookups = inject(QuestionnaireLookupsApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);

  readonly canManage = () => this.auth.hasPermission(PermissionCodes.ActionPlanManage);

  readonly surveys = signal<LookupItemDto[]>([]);
  readonly users = signal<LookupItemDto[]>([]);
  readonly lookupsBusy = signal(true);
  readonly submitBusy = signal(false);
  readonly aiPlanBusy = signal(false);

  titleAr = '';
  titleEn = '';
  descriptionAr = '';
  descriptionEn = '';
  surveyId: string | null = null;
  ownerUserId: string | null = null;
  startDate = '';
  endDate = '';

  ngOnInit(): void {
    if (!this.canManage()) {
      void this.router.navigate(['/action-plans']);
      return;
    }
    forkJoin({
      surveys: this.lookups.getSurveys('', 500).pipe(catchError(() => of([] as LookupItemDto[]))),
      users: this.lookups.getUsers('', 500).pipe(catchError(() => of([] as LookupItemDto[]))),
    }).subscribe({
      next: ({ surveys, users }) => {
        this.surveys.set(surveys);
        this.users.set(users);
        this.lookupsBusy.set(false);
      },
      error: () => this.lookupsBusy.set(false),
    });
  }

  fillWithAi(): void {
    if (!this.canManage() || this.aiPlanBusy()) return;
    const sid = this.surveyId?.trim();
    if (!sid) {
      this.toast.show(this.i18n.t('q.plans.aiSurveyRequired'), 'error');
      return;
    }
    this.aiPlanBusy.set(true);
    this.aiApi.suggestActionPlan({ surveyId: sid }).subscribe({
      next: (d) => {
        this.titleAr = d.titleAr ?? '';
        this.titleEn = d.titleEn ?? '';
        this.descriptionAr = d.descriptionAr ?? '';
        this.descriptionEn = d.descriptionEn ?? '';
        this.aiPlanBusy.set(false);
        this.toast.show(this.i18n.t('q.plans.aiFillSuccess'), 'success');
      },
      error: (err: unknown) => {
        this.aiPlanBusy.set(false);
        const msg =
          err instanceof ApiBusinessError && err.errors.length > 0
            ? err.errors[0]
            : this.i18n.t('q.plans.aiFillError');
        this.toast.show(msg, 'error');
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

  private dateToUtc(s: string): string | null {
    const t = s?.trim();
    if (!t) return null;
    const d = new Date(`${t}T12:00:00.000Z`);
    return Number.isNaN(d.getTime()) ? null : d.toISOString();
  }

  submit(): void {
    const tAr = this.titleAr.trim();
    const tEn = this.titleEn.trim();
    if (!tAr || !tEn) {
      this.toast.show(this.i18n.t('q.plans.validationTitles'), 'error');
      return;
    }
    this.submitBusy.set(true);
    this.api
      .create({
        titleAr: tAr,
        titleEn: tEn,
        descriptionAr: this.descriptionAr.trim() || null,
        descriptionEn: this.descriptionEn.trim() || null,
        surveyId: this.surveyId,
        ownerUserId: this.ownerUserId,
        startDateUtc: this.dateToUtc(this.startDate),
        endDateUtc: this.dateToUtc(this.endDate),
      })
      .subscribe({
        next: (p) => {
          this.submitBusy.set(false);
          this.toast.show(this.i18n.t('q.plans.createSuccess'), 'success');
          void this.router.navigate(['/action-plans', p.id]);
        },
        error: (err: unknown) => {
          this.submitBusy.set(false);
          const msg =
            err instanceof ApiBusinessError && err.errors.length > 0
              ? err.errors[0]
              : this.i18n.t('q.plans.createError');
          this.toast.show(msg, 'error');
        },
      });
  }
}
