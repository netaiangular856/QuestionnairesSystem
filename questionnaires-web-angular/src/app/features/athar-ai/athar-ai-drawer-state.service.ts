import { Injectable, signal } from '@angular/core';

/** Controls the global ATHAR AI copilot drawer (floating launcher). */
@Injectable({ providedIn: 'root' })
export class AtharAiDrawerStateService {
  readonly open = signal(false);

  toggle(): void {
    this.open.update((v) => !v);
  }

  openDrawer(): void {
    this.open.set(true);
  }

  closeDrawer(): void {
    this.open.set(false);
  }
}
