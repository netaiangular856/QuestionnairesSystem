import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import { AuditLogDto, AuditLogFilterRequest } from '../shared/models/audit.models';
import { toHttpParams, unwrapApiResponse } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class AuditLogsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/audit-logs');

  getPaged(filter: AuditLogFilterRequest) {
    const params = toHttpParams({
      page: filter.page,
      pageSize: filter.pageSize,
      userId: filter.userId ?? undefined,
      action: filter.action ?? undefined,
      entityType: filter.entityType ?? undefined,
      fromUtc: filter.fromUtc ? new Date(filter.fromUtc).toISOString() : undefined,
      toUtc: filter.toUtc ? new Date(filter.toUtc).toISOString() : undefined,
    });
    return this.http
      .get<ApiResponse<PagedResult<AuditLogDto>>>(this.base, { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getById(id: string) {
    return this.http.get<ApiResponse<AuditLogDto>>(`${this.base}/${id}`).pipe(map((r) => unwrapApiResponse(r)));
  }
}
