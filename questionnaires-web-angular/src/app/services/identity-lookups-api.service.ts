import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse } from '../shared/models/api.types';
import { LookupItem } from '../shared/models/lookup.models';
import { toHttpParams, unwrapApiResponse } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class IdentityLookupsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/identity-lookups');

  getRoles(search = '', take = 200) {
    const params = toHttpParams({
      search: search || undefined,
      take,
    });
    return this.http
      .get<ApiResponse<LookupItem[]>>(`${this.base}/roles`, { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
