import { Component, HostListener, inject } from '@angular/core';
import { AuthService } from '../../../core/auth/auth.service';
import { PermissionCodes } from '../../../shared/models/permission-codes';
import { I18nService } from '../../../shared/services/i18n.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { AtharAiDrawerStateService } from '../athar-ai-drawer-state.service';

@Component({
  selector: 'app-athar-ai-floating-launcher',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './athar-ai-floating-launcher.component.html',
  styleUrl: './athar-ai-floating-launcher.component.scss',
})
export class AtharAiFloatingLauncherComponent {
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);
  readonly drawer = inject(AtharAiDrawerStateService);

  readonly canAi = this.auth.hasPermission(PermissionCodes.ReportView);

  fabAriaLabel(): string {
    return this.i18n.t('atharAi.fabAria');
  }

  fabToggleLabel(): string {
    return this.i18n.t('atharAi.fabToggle');
  }

  toggle(event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    this.drawer.toggle();
  }

  close(): void {
    this.drawer.closeDrawer();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.drawer.open()) {
      this.drawer.closeDrawer();
    }
  }

  backdropClick(): void {
    this.drawer.closeDrawer();
  }
}
