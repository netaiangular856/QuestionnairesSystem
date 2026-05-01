import { Injectable, computed, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly active = signal(0);

  readonly isLoading = computed(() => this.active() > 0);

  begin(): void {
    this.active.update((n) => n + 1);
  }

  end(): void {
    this.active.update((n) => Math.max(0, n - 1));
  }
}
