import { Component, effect, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { CurrentUserProfileService } from '../../core/services/current-user-profile.service';
import { LoadingService } from '../../core/services/loading.service';
import { ToastService } from '../../core/services/toast.service';
import { AtharAiCopilotPanelComponent } from '../../features/athar-ai/athar-ai-copilot-panel/athar-ai-copilot-panel.component';
import { AtharAiDrawerStateService } from '../../features/athar-ai/athar-ai-drawer-state.service';
import { LayoutStateService } from '../layout-state.service';
import { AtharAiFloatingLauncherComponent } from '../../features/athar-ai/athar-ai-floating-launcher/athar-ai-floating-launcher.component';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { TopbarComponent } from '../topbar/topbar.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent, AtharAiFloatingLauncherComponent, AtharAiCopilotPanelComponent],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss',
})
export class AppShellComponent {
  readonly loading = inject(LoadingService);
  readonly toast = inject(ToastService);
  readonly layout = inject(LayoutStateService);
  readonly aiDrawer = inject(AtharAiDrawerStateService);

  private readonly auth = inject(AuthService);
  private readonly currentUserProfile = inject(CurrentUserProfileService);

  constructor() {
    effect(() => {
      this.auth.sessionSnapshot();
      if (this.auth.isAuthenticated()) {
        this.currentUserProfile.refreshFromServer();
      } else {
        this.currentUserProfile.clear();
      }
    });
  }

  dismissToast(): void {
    this.toast.dismiss();
  }

  closeAi(): void {
    this.aiDrawer.closeDrawer();
  }
}
