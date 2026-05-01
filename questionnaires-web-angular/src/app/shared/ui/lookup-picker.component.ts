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
      .lookup__search {
        width: 100%;
        border-radius: 9px;
        border: 1px solid var(--color-border);
        padding: 0.45rem 0.6rem;
        margin-bottom: 0.5rem;
      }
      .lookup__list {
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
        max-height: 240px;
        overflow: auto;
      }
      .lookup__item {
        border: 1px solid var(--color-border);
        border-radius: 10px;
        padding: 0.45rem 0.6rem;
        background: #fff;
        text-align: start;
        cursor: pointer;
      }
      .lookup__item--active {
        border-color: rgba(99, 102, 241, 0.38);
        background: rgba(99, 102, 241, 0.1);
        color: #312e81;
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
