import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import {
  CreateResponseRequest,
  ParticipantDetailDto,
  ParticipantDto,
  ResponseDetailDto,
  ResponseListItemDto,
} from '../shared/models/questionnaire.models';
import { toHttpParams, unwrapApiResponse } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class SurveyParticipationApiService {
  private readonly http = inject(HttpClient);

  listParticipants(surveyId: string, page = 1, pageSize = 25) {
    const params = toHttpParams({ page, pageSize });
    return this.http
      .get<ApiResponse<PagedResult<ParticipantDto>>>(apiUrl(`/api/surveys/${surveyId}/participants`), { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  listResponses(surveyId: string, page = 1, pageSize = 25) {
    const params = toHttpParams({ page, pageSize });
    return this.http
      .get<ApiResponse<PagedResult<ResponseListItemDto>>>(apiUrl(`/api/surveys/${surveyId}/responses`), { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  createResponse(surveyId: string, body: CreateResponseRequest) {
    return this.http
      .post<ApiResponse<ResponseDetailDto>>(apiUrl(`/api/surveys/${surveyId}/responses`), body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  submitResponse(responseId: string) {
    return this.http
      .post<ApiResponse<ResponseDetailDto>>(apiUrl(`/api/responses/${responseId}/submit`), {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getResponseById(responseId: string) {
    return this.http
      .get<ApiResponse<ResponseDetailDto>>(apiUrl(`/api/responses/${responseId}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getParticipantDetail(surveyId: string, participantId: string) {
    return this.http
      .get<ApiResponse<ParticipantDetailDto>>(apiUrl(`/api/surveys/${surveyId}/participants/${participantId}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
