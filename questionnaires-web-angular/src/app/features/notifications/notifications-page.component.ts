import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { NotificationsApiService } from '../../services/notifications-api.service';
import { PagedResult } from '../../shared/models/api.types';
import { NotificationDto } from '../../shared/models/notification.models';
import { PermissionCodes } from '../../shared/models/permission-codes';
import { qLocalizedTitle } from '../../shared/questionnaires/q-display';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { I18nService } from '../../shared/services/i18n.service';
import { notificationTargetUrl } from '../../shared/utils/notification-navigation';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [FormsModule, DatePipe, TranslatePipe],
  templateUrl: './notifications-page.component.html',
  styleUrl: './notifications-page.component.scss',
})
export class NotificationsPageComponent implements OnInit {
  private readonly api = inject(NotificationsApiService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly i18n = inject(I18nService);

  get canManage(): boolean {
    return this.auth.hasPermission(PermissionCodes.NotificationManage);
  }

  page = 1;
  readonly pageSize = 20;
  search = '';
  filterRead: 'all' | 'read' | 'unread' = 'all';

  readonly result = signal<PagedResult<NotificationDto> | null>(null);
  readonly failed = signal(false);
  readonly busy = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.busy.set(true);
    const isRead =
      this.filterRead === 'all' ? undefined : this.filterRead === 'read' ? true : false;
    this.api
      .getPaged({
        page: this.page,
        pageSize: this.pageSize,
        search: this.search.trim() || null,
        isRead,
      })
      .subscribe({
        next: (r) => {
          this.result.set(r);
          this.busy.set(false);
        },
        error: () => {
          this.result.set(null);
          this.failed.set(true);
          this.busy.set(false);
        },
      });
  }

  setFilter(mode: 'all' | 'read' | 'unread'): void {
    this.filterRead = mode;
    this.page = 1;
    this.load();
  }

  nextPage(): void {
    const r = this.result();
    if (!r?.hasNextPage) return;
    this.page += 1;
    this.load();
  }

  prevPage(): void {
    if (this.page <= 1) return;
    this.page -= 1;
    this.load();
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

  openLinked(row: NotificationDto): void {
    const url = this.notifLink(row);
    if (!url) return;
    void this.router.navigateByUrl(url);
    if (!row.isRead && this.canManage) {
      this.api.markRead(row.id, { isRead: true }).subscribe({
        next: () => this.load(),
        error: () => this.toast.show(this.i18n.t('notifications.error.update'), 'error'),
      });
    }
  }

  mark(row: NotificationDto, read: boolean): void {
    if (!this.canManage) return;
    this.api.markRead(row.id, { isRead: read }).subscribe({
      next: () => {
        this.toast.show(read ? this.i18n.t('notifications.read') : this.i18n.t('notifications.unread'), 'success');
        this.load();
      },
      error: () => this.toast.show(this.i18n.t('notifications.error.update'), 'error'),
    });
  }
}
