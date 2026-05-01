import { Injectable, signal } from '@angular/core';

export type ToastTone = 'success' | 'error';

export interface ToastState {
  message: string;
  tone: ToastTone;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly state = signal<ToastState | null>(null);
  private timer: ReturnType<typeof setTimeout> | null = null;

  readonly toast = this.state.asReadonly();

  show(message: string, tone: ToastTone = 'success', durationMs = 4200): void {
    if (this.timer) clearTimeout(this.timer);
    this.state.set({ message, tone });
    this.timer = setTimeout(() => {
      this.state.set(null);
      this.timer = null;
    }, durationMs);
  }

  dismiss(): void {
    if (this.timer) clearTimeout(this.timer);
    this.timer = null;
    this.state.set(null);
  }
}
