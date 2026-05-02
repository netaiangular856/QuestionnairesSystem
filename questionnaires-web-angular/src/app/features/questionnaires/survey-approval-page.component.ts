import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { SurveysApiService } from '../../services/surveys-api.service';
import { PagedResult } from '../../shared/models/api.types';
import { PermissionCodes } from '../../shared/models/permission-codes';
import {
  SurveyListItemDto,
  SurveyStatus,
} from '../../shared/models/questionnaire.models';
import { qLocalizedTitle, qSurveyStatusKey } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

@Component({
  selector: 'app-survey-approval-page',
  standalone: true,
  imports: [FormsModule, DatePipe, TranslatePipe, RouterLink],
  templateUrl: './survey-approval-page.component.html',
  styleUrl: './survey-approval-page.component.scss',
})
export class SurveyApprovalPageComponent implements OnInit {
  private readonly api = inject(SurveysApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  readonly i18n = inject(I18nService);
  readonly auth = inject(AuthService);

  readonly canApprove = this.auth.hasPermission(PermissionCodes.SurveyApprove);

  search = '';
  page = 1;
  readonly pageSize = 20;

  readonly result = signal<PagedResult<SurveyListItemDto> | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(false);
  readonly mutationBusy = signal(false);
  readonly viewMode = signal<'table' | 'cards'>('table');

  readonly rejectOpen = signal(false);
  rejectReason = '';
  readonly contextId = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  setViewMode(mode: 'table' | 'cards'): void {
    this.viewMode.set(mode);
  }

  load(): void {
    this.failed.set(false);
    this.busy.set(true);
    this.api
      .getPendingApprovalPaged({
        page: this.page,
        pageSize: this.pageSize,
        search: this.search || null,
      })
      .subscribe({
        next: (r) => {
          this.result.set(r);
          this.busy.set(false);
        },
        error: () => {
          this.result.set(null);
          this.failed.set(true);
          this.busy.set(false);
        },
      });
  }

  titleOf(row: SurveyListItemDto): string {
    return qLocalizedTitle(this.i18n.lang(), row.titleAr, row.titleEn);
  }

  statusKey(row: SurveyListItemDto): string {
    return qSurveyStatusKey(row.status);
  }

  nextPage(): void {
    const r = this.result();
    if (!r?.hasNextPage) return;
    this.page += 1;
    this.load();
  }

  prevPage(): void {
    if (this.page <= 1) return;
    this.page -= 1;
    this.load();
  }

  approve(row: SurveyListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    this.mutationBusy.set(true);
    this.api.approve(row.id).subscribe({
      next: () => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.approval.toast.approved'), 'success');
        this.load();
      },
      error: () => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.approval.toast.approveFailed'), 'error');
      },
    });
  }

  openReject(row: SurveyListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    this.contextId.set(row.id);
    this.rejectReason = '';
    this.rejectOpen.set(true);
  }

  confirmReject(): void {
    const id = this.contextId();
    if (!id || !this.rejectReason.trim()) return;
    
    this.mutationBusy.set(true);
    this.api.reject(id, { reason: this.rejectReason.trim() }).subscribe({
      next: () => {
        this.mutationBusy.set(false);
        this.rejectOpen.set(false);
        this.toast.show(this.i18n.t('q.approval.toast.rejected'), 'success');
        this.load();
      },
      error: () => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.approval.toast.rejectFailed'), 'error');
      },
    });
  }
}
