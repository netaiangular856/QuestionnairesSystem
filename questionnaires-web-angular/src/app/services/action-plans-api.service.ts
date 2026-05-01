import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse } from '../shared/models/api.types';
import { ActionPlanDto } from '../shared/models/questionnaire.models';
import { unwrapApiResponse } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class ActionPlansApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/action-plans');

  list() {
    return this.http
      .get<ApiResponse<ActionPlanDto[]>>(this.base)
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
