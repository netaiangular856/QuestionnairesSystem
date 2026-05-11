import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { CurrentUserProfileService } from '../../core/services/current-user-profile.service';
import { ToastService } from '../../core/services/toast.service';
import { NotificationDto } from '../../shared/models/notification.models';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { NotificationsApiService } from '../../services/notifications-api.service';
import { qLocalizedTitle } from '../../shared/questionnaires/q-display';
import { notificationTargetUrl } from '../../shared/utils/notification-navigation';
import { LayoutStateService } from '../layout-state.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.scss',
})
export class TopbarComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);
  readonly layout = inject(LayoutStateService);
  readonly notificationsApi = inject(NotificationsApiService);
  readonly toast = inject(ToastService);
  readonly me = inject(CurrentUserProfileService);
  private readonly router = inject(Router);
  menuOpen = false;
  notificationsOpen = false;
  loadingNotifications = false;
  notifications: NotificationDto[] = [];
  markingId: string | null = null;
  unreadCount = 0;

  ngOnInit(): void {
    this.refreshUnreadCount();
  }

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

  notifTitle(row: NotificationDto): string {
    return qLocalizedTitle(this.i18n.lang(), row.titleAr ?? '', row.titleEn ?? '');
  }

  notifMessage(row: NotificationDto): string {
    return qLocalizedTitle(this.i18n.lang(), row.messageAr ?? '', row.messageEn ?? '');
  }

  notifLink(row: NotificationDto): string | null {
    return notificationTargetUrl(row);
  }

  openNotification(row: NotificationDto): void {
    const url = this.notifLink(row);
    if (!url) return;
    void this.router.navigateByUrl(url);
    this.closePopups();
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
          this.refreshUnreadCount();
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
          this.refreshUnreadCount();
        },
        error: () => {
          this.notifications = [];
          this.toast.show(this.i18n.t('notifications.error.list'), 'error');
        },
      });
  }

  private refreshUnreadCount(): void {
    this.notificationsApi.getPaged({ page: 1, pageSize: 1, isRead: false, search: null }).subscribe({
      next: (res) => {
        this.unreadCount = Math.max(0, Number(res.totalCount ?? 0));
      },
      error: () => {
        // Don't block UI; just hide badge if count can't be fetched.
        this.unreadCount = 0;
      },
    });
  }
}
