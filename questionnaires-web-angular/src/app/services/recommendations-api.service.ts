import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse } from '../shared/models/api.types';
import { RecommendationDto } from '../shared/models/questionnaire.models';
import { unwrapApiResponse } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class RecommendationsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/recommendations');

  list() {
    return this.http
      .get<ApiResponse<RecommendationDto[]>>(this.base)
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
