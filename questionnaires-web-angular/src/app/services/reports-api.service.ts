import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse } from '../shared/models/api.types';
import { DashboardReportDto } from '../shared/models/questionnaire.models';
import { unwrapApiResponse } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/reports');

  getDashboard() {
    return this.http
      .get<ApiResponse<DashboardReportDto>>(`${this.base}/dashboard`)
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
