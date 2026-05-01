import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ErrorDisplayService {
  readonly message = signal<string | null>(null);

  show(message: string): void {
    this.message.set(message);
  }

  clear(): void {
    this.message.set(null);
  }
}
