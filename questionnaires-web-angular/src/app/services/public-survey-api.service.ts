import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import {
  CreateResponseRequest,
  PublicSurveyListItemDto,
  PublicSurveyPageDto,
  QuestionDto,
  ResponseDetailDto,
} from '../shared/models/questionnaire.models';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import { toHttpParams, unwrapApiResponse } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class PublicSurveyApiService {
  private readonly http = inject(HttpClient);

  listCatalog(page = 1, pageSize = 25) {
    const params = toHttpParams({ page, pageSize });
    return this.http
      .get<ApiResponse<PagedResult<PublicSurveyListItemDto>>>(apiUrl('/api/public/surveys'), { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getByCode(code: string) {
    return this.http
      .get<ApiResponse<PublicSurveyPageDto>>(apiUrl(`/api/public/surveys/${encodeURIComponent(code)}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  listQuestions(code: string) {
    return this.http
      .get<ApiResponse<QuestionDto[]>>(apiUrl(`/api/public/surveys/${encodeURIComponent(code)}/questions`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  createResponse(code: string, body: CreateResponseRequest) {
    return this.http
      .post<ApiResponse<ResponseDetailDto>>(
        apiUrl(`/api/public/surveys/${encodeURIComponent(code)}/responses`),
        body,
      )
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  submitResponse(code: string, responseId: string) {
    return this.http
      .post<ApiResponse<ResponseDetailDto>>(
        apiUrl(`/api/public/surveys/${encodeURIComponent(code)}/responses/${responseId}/submit`),
        {},
      )
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
