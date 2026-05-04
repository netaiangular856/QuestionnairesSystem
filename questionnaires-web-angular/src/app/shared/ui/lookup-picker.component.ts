import { Component, EventEmitter, Input, Output, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LookupItem } from '../models/lookup.models';

@Component({
  selector: 'app-lookup-picker',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="lookup">
      <input
        class="lookup__search"
        type="search"
        [ngModel]="search()"
        (ngModelChange)="search.set($event)"
        [placeholder]="searchPlaceholder"
      />
      <div class="lookup__list">
        @for (item of filtered(); track item.id) {
          <button
            type="button"
            class="lookup__item"
            [class.lookup__item--active]="isSelected(item.id)"
            (click)="toggle(item.id)"
          >
            {{ item.name }}
          </button>
        }
      </div>
    </div>
  `,
  styles: [
    `
      .lookup {
        font-family: inherit;
      }
      .lookup__search {
        width: 100%;
        box-sizing: border-box;
        border-radius: 9px;
        border: 1px solid var(--color-border, #e2e8f0);
        padding: 0.5rem 0.65rem;
        margin-bottom: 0.5rem;
        font: inherit;
        font-size: 0.92rem;
        background: #ffffff;
        color: #0f172a;
      }
      .lookup__search::placeholder {
        color: #64748b;
      }
      .lookup__list {
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
        max-height: 240px;
        overflow: auto;
      }
      .lookup__item {
        display: block;
        width: 100%;
        box-sizing: border-box;
        border: 1px solid var(--color-border, #e2e8f0);
        border-radius: 10px;
        padding: 0.5rem 0.65rem;
        background: #ffffff;
        color: #0f172a;
        text-align: start;
        cursor: pointer;
        font: inherit;
        font-size: 0.9rem;
        font-weight: 500;
        line-height: 1.35;
      }
      .lookup__item:hover {
        border-color: #cbd5e1;
        background: #f8fafc;
        color: #0f172a;
      }
      .lookup__item--active {
        border-color: rgba(99, 102, 241, 0.45);
        background: rgba(99, 102, 241, 0.12);
        color: #312e81;
        font-weight: 600;
      }
    `,
  ],
})
export class LookupPickerComponent {
  private readonly source = signal<LookupItem[]>([]);
  readonly search = signal('');

  readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    if (!term) return this.source();
    return this.source().filter((x) => x.name.toLowerCase().includes(term));
  });

  @Input({ required: true })
  set items(value: LookupItem[]) {
    this.source.set(value ?? []);
  }

  @Input() selectedIds: string[] = [];
  @Output() readonly selectedIdsChange = new EventEmitter<string[]>();

  @Input() searchPlaceholder = 'Search...';

  isSelected(id: string): boolean {
    return this.selectedIds.includes(id);
  }

  toggle(id: string): void {
    const next = new Set(this.selectedIds);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.selectedIdsChange.emit([...next]);
  }
}
