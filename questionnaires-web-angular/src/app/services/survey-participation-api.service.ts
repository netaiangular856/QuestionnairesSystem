import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import { ParticipantDetailDto, ParticipantDto, ResponseListItemDto } from '../shared/models/questionnaire.models';
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

  getParticipantDetail(surveyId: string, participantId: string) {
    return this.http
      .get<ApiResponse<ParticipantDetailDto>>(apiUrl(`/api/surveys/${surveyId}/participants/${participantId}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
