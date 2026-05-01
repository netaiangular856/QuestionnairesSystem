import { NgClass } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { TemplatesApiService } from '../../services/templates-api.service';
import { TemplateListItemDto } from '../../shared/models/questionnaire.models';
import { qLocalizedTitle } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';

@Component({
  selector: 'app-templates-page',
  standalone: true,
  imports: [TranslatePipe, NgClass],
  templateUrl: './templates-page.component.html',
  styleUrl: './templates-page.component.scss',
})
export class TemplatesPageComponent implements OnInit {
  private readonly api = inject(TemplatesApiService);
  readonly i18n = inject(I18nService);

  readonly items = signal<TemplateListItemDto[]>([]);
  readonly failed = signal(false);
  readonly busy = signal(true);

  ngOnInit(): void {
    this.api.list(false).subscribe({
      next: (rows) => {
        this.items.set(rows);
        this.busy.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.busy.set(false);
      },
    });
  }

  titleOf(t: TemplateListItemDto): string {
    return qLocalizedTitle(this.i18n.lang(), t.nameAr, t.nameEn);
  }

  cardVariant(i: number): string {
    const v = i % 3;
    if (v === 0) return 'tpl-card--ribbon';
    if (v === 1) return 'tpl-card--inset';
    return 'tpl-card--stamp';
  }
}
