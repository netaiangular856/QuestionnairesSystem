import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { TemplatesApiService } from '../../services/templates-api.service';
import { TemplateListItemDto } from '../../shared/models/questionnaire.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { qLocalizedTitle } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

type TemplateViewMode = 'table' | 'cards';

@Component({
  selector: 'app-templates-page',
  standalone: true,
  imports: [TranslatePipe, FormsModule, RouterLink],
  templateUrl: './templates-page.component.html',
  styleUrl: './templates-page.component.scss',
})
export class TemplatesPageComponent implements OnInit {
  private readonly api = inject(TemplatesApiService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);
  readonly auth = inject(AuthService);

  readonly canManage = this.auth.hasPermission(PermissionCodes.TemplateManage);

  readonly items = signal<TemplateListItemDto[]>([]);
  readonly failed = signal(false);
  readonly busy = signal(true);

  search = '';
  includeArchived = false;

  readonly viewMode = signal<TemplateViewMode>('cards');

  readonly filteredItems = computed(() => {
    const term = this.search.trim().toLowerCase();
    const rows = this.items();
    if (!term) return rows;
    return rows.filter(
      (r) =>
        r.nameAr.toLowerCase().includes(term) ||
        r.nameEn.toLowerCase().includes(term),
    );
  });

  readonly mutationBusy = signal(false);

  readonly deleteOpen = signal(false);
  deleteContext: TemplateListItemDto | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.busy.set(true);
    this.api.list(this.includeArchived).subscribe({
      next: (rows) => {
        this.items.set(rows ?? []);
        this.busy.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.busy.set(false);
      },
    });
  }

  goToCreate(): void {
    void this.router.navigate(['/templates/new']);
  }

  goToEdit(row: TemplateListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    if (!this.canManage) return;
    void this.router.navigate(['/templates', row.id, 'edit']);
  }

  goToDetail(row: TemplateListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    void this.router.navigate(['/templates', row.id]);
  }

  setViewMode(mode: TemplateViewMode): void {
    this.viewMode.set(mode);
  }

  titleOf(t: TemplateListItemDto): string {
    return qLocalizedTitle(this.i18n.lang(), t.nameAr, t.nameEn);
  }

  openDelete(row: TemplateListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    this.deleteContext = row;
    this.deleteOpen.set(true);
  }

  confirmDelete(): void {
    const row = this.deleteContext;
    if (!row) return;
    this.mutationBusy.set(true);
    this.api.delete(row.id).subscribe({
      next: () => {
        this.mutationBusy.set(false);
        this.deleteOpen.set(false);
        this.deleteContext = null;
        this.toast.show(this.i18n.t('q.templates.toast.deleted'), 'success');
        this.load();
      },
      error: () => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.templates.toast.deleteFailed'), 'error');
      },
    });
  }

  archive(row: TemplateListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    this.mutationBusy.set(true);
    this.api.archive(row.id).subscribe({
      next: () => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.templates.toast.archived'), 'success');
        this.load();
      },
      error: () => {
        this.mutationBusy.set(false);
        this.toast.show(this.i18n.t('q.templates.toast.archiveFailed'), 'error');
      },
    });
  }

  useTemplate(row: TemplateListItemDto, ev?: Event): void {
    ev?.stopPropagation();
    if (row.isArchived) return;
    this.mutationBusy.set(true);
    this.api.use(row.id).subscribe({
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
}
