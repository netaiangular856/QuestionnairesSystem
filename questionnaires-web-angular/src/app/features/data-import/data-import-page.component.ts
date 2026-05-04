import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import {
  ExcelBulkApiService,
  ExcelImportResultDto,
  ExcelTemplateScopeParam,
} from '../../services/excel-bulk-api.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { ToastService } from '../../core/services/toast.service';
import { I18nService } from '../../shared/services/i18n.service';

@Component({
  selector: 'app-data-import-page',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './data-import-page.component.html',
  styleUrl: './data-import-page.component.scss',
})
export class DataImportPageComponent {
  private readonly api = inject(ExcelBulkApiService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(I18nService);

  readonly downloading = signal(false);
  readonly importing = signal(false);
  readonly lastResult = signal<ExcelImportResultDto | null>(null);

  readonly templateScope = signal<ExcelTemplateScopeParam>('all');

  readonly scopeOptions: { value: ExcelTemplateScopeParam; labelKey: string }[] = [
    { value: 'all', labelKey: 'dataImport.scope.all' },
    { value: 'departments', labelKey: 'dataImport.scope.departments' },
    { value: 'employees', labelKey: 'dataImport.scope.employees' },
    { value: 'partners', labelKey: 'dataImport.scope.partners' },
    { value: 'users', labelKey: 'dataImport.scope.users' },
    { value: 'templates', labelKey: 'dataImport.scope.templates' },
    { value: 'surveys', labelKey: 'dataImport.scope.surveys' },
  ];

  onScopeChange(ev: Event): void {
    const v = (ev.target as HTMLSelectElement).value as ExcelTemplateScopeParam;
    this.templateScope.set(v);
  }

  download(includeSamples: boolean): void {
    const scope = this.templateScope();
    this.downloading.set(true);
    this.api.downloadTemplate(scope, includeSamples).subscribe({
      next: (blob) => {
        const suf = includeSamples ? 'sample' : 'empty';
        const name = `questionnaires-template-${scope}-${suf}.xlsx`;
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = name;
        a.click();
        URL.revokeObjectURL(url);
        this.downloading.set(false);
      },
      error: () => {
        this.downloading.set(false);
        this.toast.show(this.i18n.t('dataImport.downloadError'), 'error');
      },
    });
  }

  onFileSelected(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    if (!file.name.toLowerCase().endsWith('.xlsx')) {
      this.toast.show(this.i18n.t('dataImport.onlyXlsx'), 'error');
      return;
    }
    this.importing.set(true);
    this.lastResult.set(null);
    this.api.import(file).subscribe({
      next: (resp) => {
        this.importing.set(false);
        const d = resp.data;
        if (!d) {
          this.toast.show(this.i18n.t('dataImport.importFail'), 'error');
          return;
        }

        this.lastResult.set(d);
        if (d.errors?.length) {
          this.toast.show(this.i18n.t('dataImport.importWithErrors'), 'success');
        } else {
          this.toast.show(this.i18n.t('dataImport.importOk'), 'success');
        }
      },
      error: () => {
        this.importing.set(false);
        this.toast.show(this.i18n.t('dataImport.importFail'), 'error');
      },
    });
  }
}
