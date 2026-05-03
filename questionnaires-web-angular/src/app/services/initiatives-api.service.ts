import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import {
  AddInitiativeProgressRequest,
  InitiativeDto,
  InitiativeListItemDto,
  InitiativeProgressDto,
  UpdateInitiativeRequest,
} from '../shared/models/questionnaire.models';
import { toHttpParams, unwrapApiResponse } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class InitiativesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/initiatives');

  listPaged(page: number, pageSize: number) {
    return this.http
      .get<ApiResponse<PagedResult<InitiativeListItemDto>>>(this.base, {
        params: toHttpParams({ page, pageSize }),
      })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getById(id: string) {
    return this.http
      .get<ApiResponse<InitiativeDto>>(`${this.base}/${id}`)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  update(id: string, body: UpdateInitiativeRequest) {
    return this.http
      .put<ApiResponse<InitiativeDto>>(`${this.base}/${id}`, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  listProgress(id: string) {
    return this.http
      .get<ApiResponse<InitiativeProgressDto[]>>(`${this.base}/${id}/progress`)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  addProgress(id: string, body: AddInitiativeProgressRequest) {
    return this.http
      .post<ApiResponse<InitiativeProgressDto>>(`${this.base}/${id}/progress`, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
