import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse } from '../shared/models/api.types';
import {
  CreateTemplateRequest,
  TemplateDetailDto,
  TemplateListItemDto,
  UpdateTemplateRequest,
  UseTemplateResultDto,
} from '../shared/models/questionnaire.models';
import { toHttpParams, unwrapApiResponse, unwrapApiVoid } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class TemplatesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/templates');

  list(includeArchived = false) {
    const params = toHttpParams({ includeArchived });
    return this.http
      .get<ApiResponse<TemplateListItemDto[]>>(this.base, { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getById(id: string) {
    return this.http
      .get<ApiResponse<TemplateDetailDto>>(`${this.base}/${id}`)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  create(body: CreateTemplateRequest) {
    return this.http.post<ApiResponse<TemplateDetailDto>>(this.base, body).pipe(map((r) => unwrapApiResponse(r)));
  }

  update(id: string, body: UpdateTemplateRequest) {
    return this.http
      .put<ApiResponse<TemplateDetailDto>>(`${this.base}/${id}`, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  delete(id: string) {
    return this.http.delete<ApiResponse<unknown>>(`${this.base}/${id}`).pipe(map((r) => unwrapApiVoid(r)));
  }

  archive(id: string) {
    return this.http
      .patch<ApiResponse<TemplateDetailDto>>(`${this.base}/${id}/archive`, {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  use(id: string) {
    return this.http
      .post<ApiResponse<UseTemplateResultDto>>(`${this.base}/${id}/use`, {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
