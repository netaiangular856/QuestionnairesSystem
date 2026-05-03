import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse } from '../shared/models/api.types';
import { CrossSurveyAnalyticsDto, CrossSurveyAnalyticsFilterRequest, DashboardReportDto } from '../shared/models/questionnaire.models';
import { toHttpParams, unwrapApiResponse } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/reports');

  getDashboard() {
    return this.http
      .get<ApiResponse<DashboardReportDto>>(`${this.base}/dashboard`)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getCrossSurveyAnalytics(filter: CrossSurveyAnalyticsFilterRequest) {
    return this.http
      .get<ApiResponse<CrossSurveyAnalyticsDto>>(`${this.base}/survey-analytics`, {
        params: toHttpParams({
          surveyId: filter.surveyId || undefined,
          fromUtc: filter.fromUtc || undefined,
          toUtc: filter.toUtc || undefined,
        }),
      })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  exportCrossSurveyAnalyticsPdf(filter: CrossSurveyAnalyticsFilterRequest) {
    return this.http.get(`${this.base}/survey-analytics/export/pdf`, {
      params: toHttpParams({
        surveyId: filter.surveyId || undefined,
        fromUtc: filter.fromUtc || undefined,
        toUtc: filter.toUtc || undefined,
        lang: filter.lang || undefined,
      }),
      responseType: 'blob',
      observe: 'response',
    }) as Observable<HttpResponse<Blob>>;
  }

  exportCrossSurveyAnalyticsExcel(filter: CrossSurveyAnalyticsFilterRequest) {
    return this.http.get(`${this.base}/survey-analytics/export/excel`, {
      params: toHttpParams({
        surveyId: filter.surveyId || undefined,
        fromUtc: filter.fromUtc || undefined,
        toUtc: filter.toUtc || undefined,
        lang: filter.lang || undefined,
      }),
      responseType: 'blob',
      observe: 'response',
    }) as Observable<HttpResponse<Blob>>;
  }
}
