import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { skipGlobalLoadingHttpOptions } from '../core/http/skip-global-loading';
import { ApiResponse } from '../shared/models/api.types';
import {
  LookupItemDto,
  SurveyAudienceLookupItemDto,
} from '../shared/models/questionnaire.models';
import { toHttpParams, unwrapApiResponse } from '../shared/utils/api-helpers';

/** GET /api/lookups/* — lightweight lists for dropdowns (requires FORM_LOOKUPS / ReportView / SurveyView, etc.). */
@Injectable({ providedIn: 'root' })
export class QuestionnaireLookupsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/lookups');

  /** Survey picker; backed by QuestionnaireLookupsController GET surveys. */
  getSurveys(search?: string | null, take = 500) {
    const params = toHttpParams({
      search: search?.trim() || undefined,
      take: Math.min(500, Math.max(1, take)),
    });
    return this.http
      .get<ApiResponse<LookupItemDto[]>>(`${this.base}/surveys`, { params, ...skipGlobalLoadingHttpOptions })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  /** Active users for assignment fields (`assignedToUserId`). */
  getUsers(search?: string | null, take = 500, activeOnly = true) {
    const params = toHttpParams({
      search: search?.trim() || undefined,
      take: Math.min(500, Math.max(1, take)),
      activeOnly,
    });
    return this.http
      .get<ApiResponse<LookupItemDto[]>>(`${this.base}/users`, { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  /** Users, employees (no linked account + email), partners with email — for survey SpecificUsers audience. */
  searchSurveyAudienceSubjects(search?: string | null, take = 60) {
    const params = toHttpParams({
      search: search?.trim() || undefined,
      take: Math.min(200, Math.max(1, take)),
    });
    return this.http
      .get<ApiResponse<SurveyAudienceLookupItemDto[]>>(`${this.base}/survey-audience-subjects`, { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
