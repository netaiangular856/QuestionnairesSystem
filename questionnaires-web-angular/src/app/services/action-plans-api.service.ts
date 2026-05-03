import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import {
  ActionPlanDto,
  CreateActionPlanRequest,
  CreateInitiativeRequest,
  InitiativeDto,
  UpdateActionPlanRequest,
} from '../shared/models/questionnaire.models';
import { toHttpParams, unwrapApiResponse } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class ActionPlansApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/action-plans');

  listPaged(page: number, pageSize: number) {
    return this.http
      .get<ApiResponse<PagedResult<ActionPlanDto>>>(this.base, {
        params: toHttpParams({ page, pageSize }),
      })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getById(id: string) {
    return this.http
      .get<ApiResponse<ActionPlanDto>>(`${this.base}/${id}`)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  create(body: CreateActionPlanRequest) {
    return this.http
      .post<ApiResponse<ActionPlanDto>>(this.base, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  update(id: string, body: UpdateActionPlanRequest) {
    return this.http
      .put<ApiResponse<ActionPlanDto>>(`${this.base}/${id}`, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  listInitiatives(planId: string) {
    return this.http
      .get<ApiResponse<InitiativeDto[]>>(`${this.base}/${planId}/initiatives`)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  addInitiative(planId: string, body: CreateInitiativeRequest) {
    return this.http
      .post<ApiResponse<InitiativeDto>>(`${this.base}/${planId}/initiatives`, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
