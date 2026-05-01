import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import { MarkNotificationReadRequest, NotificationDto, NotificationFilterRequest } from '../shared/models/notification.models';
import { toHttpParams, unwrapApiResponse, unwrapApiVoid } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/notifications');

  getPaged(filter: NotificationFilterRequest) {
    const params = toHttpParams({
      page: filter.page,
      pageSize: filter.pageSize,
      isRead: filter.isRead === null || filter.isRead === undefined ? undefined : filter.isRead,
      search: filter.search ?? undefined,
    });
    return this.http
      .get<ApiResponse<PagedResult<NotificationDto>>>(this.base, { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  markRead(id: string, body: MarkNotificationReadRequest) {
    return this.http
      .patch<ApiResponse<unknown>>(`${this.base}/${id}/read`, body)
      .pipe(map((r) => unwrapApiVoid(r)));
  }
}
