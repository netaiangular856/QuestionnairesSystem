import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-searchable-select',
  standalone: true,
  template: `<input [placeholder]="placeholder" style="width:100%;padding:.5rem" />`,
})
export class SearchableSelectComponent {
  @Input() placeholder = '';
}
