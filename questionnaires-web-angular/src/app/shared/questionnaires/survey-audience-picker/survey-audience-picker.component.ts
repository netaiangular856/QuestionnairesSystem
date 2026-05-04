import { Component, EventEmitter, Input, OnDestroy, Output, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { QuestionnaireLookupsApiService } from '../../../services/questionnaire-lookups-api.service';
import {
  SurveyAudienceLookupItemDto,
  SurveyAudiencePickItem,
  SurveyAudienceSubjectKind,
} from '../../models/questionnaire.models';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { I18nService } from '../../services/i18n.service';

@Component({
  selector: 'app-survey-audience-picker',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  templateUrl: './survey-audience-picker.component.html',
  styleUrl: './survey-audience-picker.component.scss',
})
export class SurveyAudiencePickerComponent implements OnDestroy {
  private readonly lookups = inject(QuestionnaireLookupsApiService);
  readonly i18n = inject(I18nService);

  @Input() disabled = false;
  @Input() selection: SurveyAudiencePickItem[] = [];
  @Output() selectionChange = new EventEmitter<SurveyAudiencePickItem[]>();

  searchInput = '';
  readonly results = signal<SurveyAudienceLookupItemDto[]>([]);
  readonly searchBusy = signal(false);
  /** After any request finished — distinguish «focus to load» vs «no rows in DB». */
  readonly hasFetched = signal(false);

  private readonly searchTerms = new Subject<string>();
  private readonly sub: Subscription;

  constructor() {
    this.sub = this.searchTerms
      .pipe(
        debounceTime(320),
        distinctUntilChanged(),
        switchMap((q) => {
          this.searchBusy.set(true);
          return this.lookups.searchSurveyAudienceSubjects(q || undefined);
        }),
      )
      .subscribe({
        next: (rows) => {
          this.searchBusy.set(false);
          this.results.set(rows ?? []);
          this.hasFetched.set(true);
        },
        error: () => {
          this.searchBusy.set(false);
          this.results.set([]);
          this.hasFetched.set(true);
        },
      });
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  onSearchInput(): void {
    this.searchTerms.next(this.searchInput.trim());
  }

  runSearch(): void {
    this.searchTerms.next(this.searchInput.trim());
  }

  /** Load first page of suggestions as soon as the field is focused (no typing required). */
  loadOnFocus(): void {
    if (this.disabled) return;
    this.searchBusy.set(true);
    this.lookups.searchSurveyAudienceSubjects(this.searchInput.trim() || undefined).subscribe({
      next: (rows) => {
        this.searchBusy.set(false);
        this.results.set(rows ?? []);
        this.hasFetched.set(true);
      },
      error: () => {
        this.searchBusy.set(false);
        this.results.set([]);
        this.hasFetched.set(true);
      },
    });
  }

  kindLabel(kind: SurveyAudienceSubjectKind): string {
    switch (kind) {
      case SurveyAudienceSubjectKind.User:
        return this.i18n.t('q.surveys.audience.kindUser');
      case SurveyAudienceSubjectKind.Employee:
        return this.i18n.t('q.surveys.audience.kindEmployee');
      case SurveyAudienceSubjectKind.Partner:
        return this.i18n.t('q.surveys.audience.kindPartner');
      default:
        return '';
    }
  }

  pickKey(p: SurveyAudiencePickItem): string {
    if (p.userId) return `u:${p.userId}`;
    if (p.email) return `e:${p.email.toLowerCase()}`;
    return `x:${p.entityId}`;
  }

  addFromLookup(row: SurveyAudienceLookupItemDto): void {
    const userId = row.userId?.trim() || null;
    const email = userId ? null : row.email?.trim() || null;
    if (!userId && !email) return;

    const label = `${row.name} (${this.kindLabel(row.kind)})`;
    const item: SurveyAudiencePickItem = {
      userId,
      email,
      label,
      kind: row.kind,
      entityId: row.entityId,
    };
    const key = this.pickKey(item);
    if (this.selection.some((x) => this.pickKey(x) === key)) return;
    this.selectionChange.emit([...this.selection, item]);
  }

  removeAt(i: number): void {
    this.selectionChange.emit(this.selection.filter((_, j) => j !== i));
  }
}
