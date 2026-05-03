import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import {
  CreateRecommendationRequest,
  RecommendationDto,
  UpdateRecommendationRequest,
} from '../shared/models/questionnaire.models';
import { toHttpParams, unwrapApiResponse, unwrapApiVoid } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class RecommendationsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/recommendations');

  /** GET /api/recommendations?page=&pageSize= */
  listPaged(page: number, pageSize: number) {
    const params = toHttpParams({ page, pageSize });
    return this.http
      .get<ApiResponse<PagedResult<RecommendationDto>>>(this.base, { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  /** GET /api/recommendations/{id} */
  getById(id: string) {
    return this.http
      .get<ApiResponse<RecommendationDto>>(`${this.base}/${id}`)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  create(body: CreateRecommendationRequest) {
    return this.http
      .post<ApiResponse<RecommendationDto>>(this.base, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  /** PUT /api/recommendations/{id} */
  update(id: string, body: UpdateRecommendationRequest) {
    return this.http
      .put<ApiResponse<RecommendationDto>>(`${this.base}/${id}`, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  /** DELETE /api/recommendations/{id} */
  delete(id: string) {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.base}/${id}`)
      .pipe(map((r) => unwrapApiVoid(r)));
  }
}
