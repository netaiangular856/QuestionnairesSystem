import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { SurveysApiService } from '../../../../services/surveys-api.service';
import { PagedResult } from '../../../../shared/models/api.types';
import { PermissionCodes } from '../../../../shared/models/permission-codes';
import {
  SurveyAudienceScope,
  SurveyListItemDto,
  SurveyStatus,
  UpdateSurveyRequest,
} from '../../../../shared/models/questionnaire.models';
import { qLocalizedTitle, qSurveyStatusKey } from '../../../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';

type SurveyViewMode = 'table' | 'cards';

@Component({
  selector: 'app-surveys-page',
  standalone: true,
  imports: [FormsModule, DatePipe, TranslatePipe, RouterLink],
  templateUrl: './surveys-page.component.html',
  styleUrl: './surveys-page.component.scss',
})
export class SurveysPageComponent implements OnInit {
  private readonly api = inject(SurveysApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  readonly i18n = inject(I18nService);
  readonly auth = inject(AuthService);

  readonly canManage = this.auth.hasPermission(PermissionCodes.SurveyManage);
  readonly canApproveWorkflow = this.auth.hasAnyPermission([
    PermissionCodes.SurveyManage,
    PermissionCodes.SurveyApprove,
  ]);

  search = '';
  page = 1;
  readonly pageSize = 20;

  readonly result = signal<PagedResult<SurveyListItemDto> | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(false);

  readonly viewMode = signal<SurveyViewMode>('table');

  readonly deleteOpen = signal(false);
  readonly patchOpen = signal(false);
  readonly rejectOpen = signal(false);
  rejectReason = '';
  patchStatus: SurveyStatus = SurveyStatus.Draft;

  /** Active survey id for row-driven modals and mutations */
  readonly contextId = signal<string | null>(null);

  readonly mutationBusy = signal(false);

  readonly SurveyAudienceScope = SurveyAudienceScope;
  readonly SurveyStatus = SurveyStatus;

  ngOnInit(): void {
    this.load();
  }

  private emptyEditModel(): UpdateSurveyRequest {
    return {
      titleAr: '',
      titleEn: '',
      descriptionAr: '',
      descriptionEn: '',
      code: '',
      audienceScope: SurveyAudienceScope.AllOrganizationMembers,
    };
  }

  load(): void {
    this.failed.set(false);
    this.busy.set(true);
    this.api
      .getPaged({
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

  /** Matches `.rec-card--*` accents from recommendations */
  surveyCardClass(row: SurveyListItemDto): string {
    switch (row.status) {
      case SurveyStatus.Published:
        return 'rec-card--implemented';
      case SurveyStatus.Closed:
      case SurveyStatus.Rejected:
        return 'rec-card--dismissed';
      case SurveyStatus.PendingApproval:
      case SurveyStatus.Approved:
        return 'rec-card--active';
      default:
        return 'rec-card--draft';
    }
  }

  surveyStatusVariant(row: SurveyListItemDto): string {
    switch (row.status) {
      case SurveyStatus.Published:
        return 'implemented';
      case SurveyStatus.Closed:
      case SurveyStatus.Rejected:
        return 'dismissed';
      case SurveyStatus.PendingApproval:
      case SurveyStatus.Approved:
        return 'active';
      default:
        return 'draft';
    }
  }

  setViewMode(mode: SurveyViewMode): void {
    this.viewMode.set(mode);
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

  goToCreate(): void {
    void this.router.navigate(['/surveys/new']);
  }

  showSubmit(row: SurveyListItemDto): boolean {
    return this.canManage && (row.status === SurveyStatus.Draft || row.status === SurveyStatus.Rejected);
  }

  showApproveReject(row: SurveyListItemDto): boolean {
    return this.canApproveWorkflow && row.status === SurveyStatus.PendingApproval;
  }

  showPublish(row: SurveyListItemDto): boolean {
    return this.canManage && row.status === SurveyStatus.Approved;
  }

  showClose(row: SurveyListItemDto): boolean {
    return this.canManage && row.status === SurveyStatus.Published;
  }

  openEditRow(row: SurveyListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    void this.router.navigate(['/surveys', row.id, 'edit']);
  }

  openDeleteRow(row: SurveyListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    this.contextId.set(row.id);
    this.deleteOpen.set(true);
  }

  confirmDelete(): void {
    const id = this.contextId();
    if (!id) return;
    this.mutationBusy.set(true);
    this.api.delete(id).subscribe({
      next: () => {
        this.mutationBusy.set(false);
        this.deleteOpen.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.deleted'), 'success');
        this.load();
      },
      error: () => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.deleteFailed'), 'error');
      },
    });
  }

  duplicateRow(row: SurveyListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    this.mutationBusy.set(true);
    this.api.duplicate(row.id).subscribe({
      next: (s) => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.duplicated'), 'success');
        void this.router.navigate(['/surveys', s.id]);
      },
      error: () => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.duplicateFailed'), 'error');
      },
    });
  }

  openPatchRow(row: SurveyListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    this.contextId.set(row.id);
    this.patchStatus = row.status;
    this.patchOpen.set(true);
  }

  applyPatch(): void {
    const id = this.contextId();
    if (!id) return;
    this.mutationBusy.set(true);
    this.api.patchStatus(id, { status: this.patchStatus }).subscribe({
      next: () => {
        this.mutationBusy.set(false);
        this.patchOpen.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.transitionOk'), 'success');
        this.load();
      },
      error: () => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.transitionFailed'), 'error');
      },
    });
  }

  openRejectRow(row: SurveyListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    this.contextId.set(row.id);
    this.rejectReason = '';
    this.rejectOpen.set(true);
  }

  submitReject(): void {
    const id = this.contextId();
    if (!id || !this.rejectReason.trim()) {
      this.toast.show(this.i18n.t('q.detail.toast.rejectReason'), 'error');
      return;
    }
    this.mutationBusy.set(true);
    this.api.reject(id, { reason: this.rejectReason.trim() }).subscribe({
      next: () => {
        this.mutationBusy.set(false);
        this.rejectOpen.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.transitionOk'), 'success');
        this.load();
      },
      error: () => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.transitionFailed'), 'error');
      },
    });
  }

  submitForApprovalRow(row: SurveyListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    this.runTransition(() => this.api.submitForApproval(row.id));
  }

  approveRow(row: SurveyListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    this.runTransition(() => this.api.approve(row.id));
  }

  publishRow(row: SurveyListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    this.runTransition(() => this.api.publish(row.id));
  }

  closeRow(row: SurveyListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    this.runTransition(() => this.api.close(row.id));
  }

  private runTransition(req: () => Observable<unknown>): void {
    this.mutationBusy.set(true);
    req().subscribe({
      next: () => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.transitionOk'), 'success');
        this.load();
      },
      error: () => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.toast.transitionFailed'), 'error');
      },
    });
  }
}
