import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ToastService } from '../../../../core/services/toast.service';
import { SurveysApiService } from '../../../../services/surveys-api.service';
import { PagedResult } from '../../../../shared/models/api.types';
import {
  SurveyListItemDto,
} from '../../../../shared/models/questionnaire.models';
import { qLocalizedTitle } from '../../../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { I18nService } from '../../../../shared/services/i18n.service';

@Component({
  selector: 'app-available-surveys-page',
  standalone: true,
  imports: [FormsModule, DatePipe, TranslatePipe],
  templateUrl: './available-surveys-page.component.html',
  styleUrl: './available-surveys-page.component.scss',
})
export class AvailableSurveysPageComponent implements OnInit {
  private readonly api = inject(SurveysApiService);
  private readonly toast = inject(ToastService);
  readonly router = inject(Router);
  readonly i18n = inject(I18nService);

  search = '';
  page = 1;
  readonly pageSize = 12;

  readonly result = signal<PagedResult<SurveyListItemDto> | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(false);
  readonly viewMode = signal<'table' | 'cards'>('cards');

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
      .getAvailablePaged({
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

  fillSurvey(row: SurveyListItemDto): void {
    void this.router.navigate(['/surveys', row.id, 'fill']);
  }
}
