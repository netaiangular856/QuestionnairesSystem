import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToastService } from '../../core/services/toast.service';
import { AuditLogsApiService } from '../../services/audit-logs-api.service';
import { AuditLogDto } from '../../shared/models/audit.models';
import { PagedResult } from '../../shared/models/api.types';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { openDatetimeLocalPicker } from '../../shared/utils/open-datetime-local-picker';

@Component({
  selector: 'app-audit-logs-page',
  standalone: true,
  imports: [FormsModule, DatePipe, TranslatePipe],
  templateUrl: './audit-logs-page.component.html',
  styleUrl: './audit-logs-page.component.scss',
})
export class AuditLogsPageComponent implements OnInit {
  private readonly api = inject(AuditLogsApiService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  page = 1;
  readonly pageSize = 20;
  search = '';
  actionFilter = '';
  entityFilter = '';
  fromUtc: string | null = null;
  toUtc: string | null = null;

  /** HTTP methods stored by the API (see AuditTrailMiddleware). */
  readonly actionOptions = ['POST', 'PUT', 'PATCH', 'DELETE'] as const;

  /** Path prefixes used in EntityType filters (Contains match on full path). */
  readonly entityPathOptions: readonly string[] = [
    '/api/public/surveys',
    '/api/surveys',
    '/api/responses',
    '/api/recommendations',
    '/api/action-plans',
    '/api/initiatives',
    '/api/questions',
    '/api/templates',
    '/api/reports',
    '/api/notifications',
    '/api/lookups',
    '/api/excel-bulk',
    '/api/ai',
    '/api/departments',
    '/api/employees',
    '/api/partners',
    '/api/users',
    '/api/roles',
    '/api/permissions',
    '/api/identity-lookups',
    '/api/account',
    '/api/audit-logs',
  ];

  readonly result = signal<PagedResult<AuditLogDto> | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(false);

  readonly detailOpen = signal(false);
  readonly detailBusy = signal(false);
  readonly detailLog = signal<AuditLogDto | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.busy.set(true);
    this.api
      .getPaged({
        page: this.page,
        pageSize: this.pageSize,
        action: this.actionFilter.trim() || null,
        entityType: this.entityFilter.trim() || null,
        fromUtc: this.fromUtc ? new Date(this.fromUtc) : null,
        toUtc: this.toUtc ? new Date(this.toUtc) : null,
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

  applySearch(): void {
    const term = this.search.trim();
    this.entityFilter = term;
    this.page = 1;
    this.load();
  }

  openPicker(input: HTMLInputElement): void {
    openDatetimeLocalPicker(input);
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

  openDetail(id: string): void {
    this.detailBusy.set(true);
    this.detailOpen.set(true);
    this.api.getById(id).subscribe({
      next: (row) => {
        this.detailLog.set(row);
        this.detailBusy.set(false);
      },
      error: () => {
        this.detailBusy.set(false);
        this.detailOpen.set(false);
        this.toast.show(this.i18n.t('audit.error.load'), 'error');
      },
    });
  }

  closeDetail(): void {
    if (this.detailBusy()) return;
    this.detailOpen.set(false);
    this.detailLog.set(null);
  }

  displayUser(row: Pick<AuditLogDto, 'userName' | 'userId'>): string {
    return row.userName?.trim() || row.userId || '—';
  }

  displayAction(action: string): string {
    const value = action.trim().toUpperCase();
    if (value === 'POST') return this.i18n.lang() === 'ar' ? 'إضافة' : 'Create';
    if (value === 'PUT') return this.i18n.lang() === 'ar' ? 'تعديل كامل' : 'Replace';
    if (value === 'PATCH') return this.i18n.lang() === 'ar' ? 'تعديل' : 'Update';
    if (value === 'DELETE') return this.i18n.lang() === 'ar' ? 'حذف' : 'Delete';
    return action;
  }

  displayEntity(entityType: string): string {
    const path = entityType.toLowerCase();
    const labels: Array<[string, string, string]> = [
      ['/api/public/surveys', 'استبيانات عامة', 'Public surveys'],
      ['/api/audit-logs', 'سجل التدقيق', 'Audit logs'],
      ['/api/identity-lookups', 'قوائم الهوية', 'Identity lookups'],
      ['/api/recommendations', 'التوصيات', 'Recommendations'],
      ['/api/action-plans', 'خطط العمل', 'Action plans'],
      ['/api/notifications', 'الإشعارات', 'Notifications'],
      ['/api/permissions', 'الصلاحيات', 'Permissions'],
      ['/api/departments', 'الإدارات', 'Departments'],
      ['/api/initiatives', 'المبادرات', 'Initiatives'],
      ['/api/excel-bulk', 'استيراد Excel', 'Excel bulk'],
      ['/api/responses', 'الإجابات', 'Responses'],
      ['/api/templates', 'القوالب', 'Templates'],
      ['/api/questions', 'الأسئلة', 'Questions'],
      ['/api/employees', 'الموظفون', 'Employees'],
      ['/api/partners', 'الشركاء', 'Partners'],
      ['/api/surveys', 'الاستبيانات', 'Surveys'],
      ['/api/reports', 'التقارير', 'Reports'],
      ['/api/lookups', 'قوائم الاستبيان', 'Questionnaire lookups'],
      ['/api/account', 'الحساب', 'Account'],
      ['/api/users', 'المستخدمون', 'Users'],
      ['/api/roles', 'الأدوار', 'Roles'],
      ['/api/auth', 'الهوية وتسجيل الدخول', 'Authentication'],
      ['/api/ai', 'الذكاء الاصطناعي', 'AI'],
    ];

    const sorted = [...labels].sort((a, b) => b[0].length - a[0].length);
    for (const [key, ar, en] of sorted) {
      if (path.includes(key)) {
        return this.i18n.lang() === 'ar' ? ar : en;
      }
    }

    return entityType;
  }
}
