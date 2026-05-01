import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { CurrentUserProfileService } from '../../core/services/current-user-profile.service';
import { ToastService } from '../../core/services/toast.service';
import { NotificationDto } from '../../shared/models/notification.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { NotificationsApiService } from '../../services/notifications-api.service';
import { LayoutStateService } from '../layout-state.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.scss',
})
export class TopbarComponent {
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);
  readonly layout = inject(LayoutStateService);
  readonly notificationsApi = inject(NotificationsApiService);
  readonly toast = inject(ToastService);
  readonly me = inject(CurrentUserProfileService);
  menuOpen = false;
  notificationsOpen = false;
  loadingNotifications = false;
  notifications: NotificationDto[] = [];
  markingId: string | null = null;

  logout(): void {
    this.menuOpen = false;
    this.auth.logout();
  }

  toggleLang(): void {
    this.closePopups();
    this.i18n.toggleLang();
  }

  toggleSidebar(): void {
    this.layout.toggleMobileSidebar();
  }

  toggleMenu(): void {
    this.menuOpen = !this.menuOpen;
    if (this.menuOpen) this.notificationsOpen = false;
  }

  toggleNotifications(): void {
    this.notificationsOpen = !this.notificationsOpen;
    if (this.notificationsOpen) this.menuOpen = false;
    if (this.notificationsOpen) this.loadTopNotifications();
  }

  closePopups(): void {
    this.menuOpen = false;
    this.notificationsOpen = false;
  }

  markAsRead(row: NotificationDto): void {
    if (row.isRead || this.markingId) return;
    this.markingId = row.id;
    this.notificationsApi
      .markRead(row.id, { isRead: true })
      .pipe(finalize(() => (this.markingId = null)))
      .subscribe({
        next: () => {
          this.notifications = this.notifications.map((n) => (n.id === row.id ? { ...n, isRead: true } : n));
        },
        error: () => this.toast.show('notifications.error.update', 'error'),
      });
  }

  private loadTopNotifications(): void {
    this.loadingNotifications = true;
    this.notificationsApi
      .getPaged({ page: 1, pageSize: 5, isRead: null, search: null })
      .pipe(finalize(() => (this.loadingNotifications = false)))
      .subscribe({
        next: (res) => {
          this.notifications = [...res.items];
        },
        error: () => {
          this.notifications = [];
          this.toast.show(this.i18n.t('notifications.error.list'), 'error');
        },
      });
  }
}
