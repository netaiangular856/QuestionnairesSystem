import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse } from '../../shared/models/api.types';
import { unwrapApiResponse } from '../../shared/utils/api-helpers';
import type { UserProfileDto } from '../../shared/models/profile.models';

@Injectable({ providedIn: 'root' })
export class ProfileApiService {
  private readonly http = inject(HttpClient);

  private static readonly skipLoadingHeaders = new HttpHeaders({ 'X-Skip-Loading': '1' });

  getMine() {
    return this.http
      .get<ApiResponse<UserProfileDto>>(apiUrl('/api/account/profile'), {
        headers: ProfileApiService.skipLoadingHeaders,
      })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  updateMine(body: { nameAr?: string | null; nameEn?: string | null; email: string }) {
    return this.http
      .put<ApiResponse<UserProfileDto>>(apiUrl('/api/account/profile'), body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  uploadAvatar(file: File) {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http
      .post<ApiResponse<UserProfileDto>>(apiUrl('/api/account/profile/avatar'), form)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  clearAvatar() {
    return this.http
      .delete<ApiResponse<UserProfileDto>>(apiUrl('/api/account/profile/avatar'))
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
