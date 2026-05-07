import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LayoutStateService {
  readonly mobileSidebarOpen = signal(false);
  readonly sidebarCollapsed = signal(false);

  toggleMobileSidebar(): void { this.mobileSidebarOpen.update((v) => !v); }
  closeMobileSidebar(): void { this.mobileSidebarOpen.set(false); }

  toggleSidebarCollapse(): void { this.sidebarCollapsed.update((v) => !v); }
}
