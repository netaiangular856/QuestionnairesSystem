import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse } from '../shared/models/api.types';
import { TemplateListItemDto } from '../shared/models/questionnaire.models';
import { toHttpParams, unwrapApiResponse } from '../shared/utils/api-helpers';

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
}
