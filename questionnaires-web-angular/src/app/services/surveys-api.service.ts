import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import {
  CreateSurveyRequest,
  PatchSurveyStatusRequest,
  QuestionAnalyticsItemDto,
  RejectSurveyRequest,
  SurveyAnalyticsDto,
  SurveyAnalyticsSummaryDto,
  SurveyDetailDto,
  SurveyFilterRequest,
  SurveyListItemDto,
  UpdateSurveyRequest,
} from '../shared/models/questionnaire.models';
import { toHttpParams, unwrapApiResponse, unwrapApiVoid } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class SurveysApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/surveys');

  getPaged(filter: SurveyFilterRequest) {
    const params = toHttpParams({
      page: filter.page,
      pageSize: filter.pageSize,
      status: filter.status ?? undefined,
      search: filter.search || undefined,
    });
    return this.http
      .get<ApiResponse<PagedResult<SurveyListItemDto>>>(this.base, { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getById(id: string) {
    return this.http
      .get<ApiResponse<SurveyDetailDto>>(`${this.base}/${id}`)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  create(body: CreateSurveyRequest) {
    return this.http.post<ApiResponse<SurveyDetailDto>>(this.base, body).pipe(map((r) => unwrapApiResponse(r)));
  }

  update(id: string, body: UpdateSurveyRequest) {
    return this.http
      .put<ApiResponse<SurveyDetailDto>>(`${this.base}/${id}`, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  delete(id: string) {
    return this.http.delete<ApiResponse<unknown>>(`${this.base}/${id}`).pipe(map((r) => unwrapApiVoid(r)));
  }

  duplicate(id: string) {
    return this.http
      .post<ApiResponse<SurveyDetailDto>>(`${this.base}/${id}/duplicate`, {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  patchStatus(id: string, body: PatchSurveyStatusRequest) {
    return this.http
      .patch<ApiResponse<SurveyDetailDto>>(`${this.base}/${id}/status`, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  submitForApproval(id: string) {
    return this.http
      .post<ApiResponse<SurveyDetailDto>>(`${this.base}/${id}/submit-for-approval`, {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  approve(id: string) {
    return this.http
      .post<ApiResponse<SurveyDetailDto>>(`${this.base}/${id}/approve`, {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  reject(id: string, body: RejectSurveyRequest) {
    return this.http
      .post<ApiResponse<SurveyDetailDto>>(`${this.base}/${id}/reject`, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  publish(id: string) {
    return this.http
      .post<ApiResponse<SurveyDetailDto>>(`${this.base}/${id}/publish`, {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  close(id: string) {
    return this.http
      .post<ApiResponse<SurveyDetailDto>>(`${this.base}/${id}/close`, {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getAnalytics(id: string) {
    return this.http
      .get<ApiResponse<SurveyAnalyticsDto>>(`${this.base}/${id}/analytics`)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getAnalyticsSummary(id: string) {
    return this.http
      .get<ApiResponse<SurveyAnalyticsSummaryDto>>(`${this.base}/${id}/analytics/summary`)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getQuestionAnalytics(id: string, page = 1, pageSize = 25) {
    const params = toHttpParams({ page, pageSize });
    return this.http
      .get<ApiResponse<PagedResult<QuestionAnalyticsItemDto>>>(`${this.base}/${id}/analytics/questions`, { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
