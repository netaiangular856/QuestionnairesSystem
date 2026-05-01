import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { SurveysApiService } from '../../services/surveys-api.service';
import { TemplatesApiService } from '../../services/templates-api.service';
import { PagedResult } from '../../shared/models/api.types';
import { PermissionCodes } from '../../shared/models/permission-codes';
import {
  CreateSurveyRequest,
  SurveyAudienceScope,
  SurveyListItemDto,
  SurveyStatus,
  TemplateListItemDto,
  UpdateSurveyRequest,
} from '../../shared/models/questionnaire.models';
import { qLocalizedTitle, qSurveyStatusKey } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

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
  private readonly templatesApi = inject(TemplatesApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  readonly i18n = inject(I18nService);
  readonly auth = inject(AuthService);

  readonly canManage = this.auth.hasPermission(PermissionCodes.SurveyManage);

  search = '';
  page = 1;
  readonly pageSize = 20;

  readonly result = signal<PagedResult<SurveyListItemDto> | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(false);

  readonly viewMode = signal<SurveyViewMode>('table');

  readonly templates = signal<TemplateListItemDto[]>([]);

  readonly createOpen = signal(false);
  readonly createBusy = signal(false);
  createModel: CreateSurveyRequest = this.emptyCreateModel();

  readonly editOpen = signal(false);
  readonly editBusy = signal(false);
  editModel: UpdateSurveyRequest = this.emptyEditModel();

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
    this.templatesApi.list(false).subscribe({
      next: (list) => this.templates.set(list ?? []),
      error: () => this.templates.set([]),
    });
  }

  private emptyCreateModel(): CreateSurveyRequest {
    return {
      titleAr: '',
      titleEn: '',
      descriptionAr: '',
      descriptionEn: '',
      code: '',
      audienceScope: SurveyAudienceScope.AllOrganizationMembers,
      templateId: null,
    };
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

  templateTitle(t: TemplateListItemDto): string {
    return qLocalizedTitle(this.i18n.lang(), t.nameAr, t.nameEn);
  }

  statusKey(row: SurveyListItemDto): string {
    return qSurveyStatusKey(row.status);
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

  openCreate(): void {
    this.createModel = this.emptyCreateModel();
    this.createOpen.set(true);
  }

  submitCreate(): void {
    if (!this.createModel.titleAr.trim() || !this.createModel.titleEn.trim()) {
      this.toast.show(this.i18n.t('users.toast.editRequired'), 'error');
      return;
    }
    const tid = this.createModel.templateId?.trim();
    const body: CreateSurveyRequest = {
      titleAr: this.createModel.titleAr.trim(),
      titleEn: this.createModel.titleEn.trim(),
      descriptionAr: this.createModel.descriptionAr?.trim() || null,
      descriptionEn: this.createModel.descriptionEn?.trim() || null,
      code: this.createModel.code?.trim() || null,
      audienceScope: this.createModel.audienceScope,
      templateId: tid || null,
    };
    this.createBusy.set(true);
    this.api.create(body).subscribe({
      next: () => {
        this.createBusy.set(false);
        this.createOpen.set(false);
        this.toast.show(this.i18n.t('q.surveys.toast.created'), 'success');
        this.page = 1;
        this.load();
      },
      error: () => {
        this.createBusy.set(false);
        this.toast.show(this.i18n.t('q.surveys.toast.createFailed'), 'error');
      },
    });
  }

  showSubmit(row: SurveyListItemDto): boolean {
    return this.canManage && (row.status === SurveyStatus.Draft || row.status === SurveyStatus.Rejected);
  }

  showApproveReject(row: SurveyListItemDto): boolean {
    return this.canManage && row.status === SurveyStatus.PendingApproval;
  }

  showPublish(row: SurveyListItemDto): boolean {
    return this.canManage && row.status === SurveyStatus.Approved;
  }

  showClose(row: SurveyListItemDto): boolean {
    return this.canManage && row.status === SurveyStatus.Published;
  }

  openEditRow(row: SurveyListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    this.contextId.set(row.id);
    this.editBusy.set(true);
    this.api.getById(row.id).subscribe({
      next: (s) => {
        this.editModel = {
          titleAr: s.titleAr,
          titleEn: s.titleEn,
          descriptionAr: s.descriptionAr ?? '',
          descriptionEn: s.descriptionEn ?? '',
          code: s.code ?? '',
          audienceScope: s.audienceScope,
        };
        this.editBusy.set(false);
        this.editOpen.set(true);
      },
      error: () => {
        this.editBusy.set(false);
        this.toast.show(this.i18n.t('q.detail.error'), 'error');
      },
    });
  }

  saveEdit(): void {
    const id = this.contextId();
    if (!id || !this.editModel.titleAr.trim() || !this.editModel.titleEn.trim()) {
      this.toast.show(this.i18n.t('users.toast.editRequired'), 'error');
      return;
    }
    this.mutationBusy.set(true);
    this.api
      .update(id, {
        ...this.editModel,
        titleAr: this.editModel.titleAr.trim(),
        titleEn: this.editModel.titleEn.trim(),
        descriptionAr: this.editModel.descriptionAr?.trim() || null,
        descriptionEn: this.editModel.descriptionEn?.trim() || null,
        code: this.editModel.code?.trim() || null,
      })
      .subscribe({
        next: () => {
          this.mutationBusy.set(false);
          this.editOpen.set(false);
          this.toast.show(this.i18n.t('q.detail.toast.updated'), 'success');
          this.load();
        },
        error: () => {
          this.mutationBusy.set(false);
          this.toast.show(this.i18n.t('q.detail.toast.updateFailed'), 'error');
        },
      });
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
