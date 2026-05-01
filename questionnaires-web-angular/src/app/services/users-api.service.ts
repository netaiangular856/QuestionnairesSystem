import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import {
  AssignUserRolesRequest,
  CreateUserRequest,
  UpdateUserRequest,
  UserDto,
  UserFilterRequest,
  UserListItemDto,
} from '../shared/models/user.models';
import { toHttpParams, unwrapApiResponse, unwrapApiVoid } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/users');

  getPaged(filter: UserFilterRequest) {
    const params = toHttpParams({
      page: filter.page,
      pageSize: filter.pageSize,
      search: filter.search ?? undefined,
    });
    return this.http
      .get<ApiResponse<PagedResult<UserListItemDto>>>(this.base, { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getById(id: string) {
    return this.http.get<ApiResponse<UserDto>>(`${this.base}/${id}`).pipe(map((r) => unwrapApiResponse(r)));
  }

  create(body: CreateUserRequest) {
    return this.http.post<ApiResponse<UserDto>>(this.base, body).pipe(map((r) => unwrapApiResponse(r)));
  }

  update(id: string, body: UpdateUserRequest) {
    return this.http.put<ApiResponse<UserDto>>(`${this.base}/${id}`, body).pipe(map((r) => unwrapApiResponse(r)));
  }

  patchStatus(id: string, isActive: boolean) {
    return this.http
      .patch<ApiResponse<unknown>>(`${this.base}/${id}/status`, { isActive })
      .pipe(map((r) => unwrapApiVoid(r)));
  }

  assignRoles(id: string, body: AssignUserRolesRequest) {
    return this.http.put<ApiResponse<UserDto>>(`${this.base}/${id}/roles`, body).pipe(map((r) => unwrapApiResponse(r)));
  }
}
