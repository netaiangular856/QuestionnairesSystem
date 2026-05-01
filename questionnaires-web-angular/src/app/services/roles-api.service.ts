import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import {
  AssignRolePermissionsRequest,
  CreateRoleRequest,
  PermissionDto,
  RoleDto,
  RoleFilterRequest,
  RoleListItemDto,
  UpdateRoleRequest,
} from '../shared/models/role.models';
import { toHttpParams, unwrapApiResponse } from '../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class RolesApiService {
  private readonly http = inject(HttpClient);
  private readonly rolesBase = apiUrl('/api/roles');
  private readonly permissionsBase = apiUrl('/api/permissions');

  getPaged(filter: RoleFilterRequest) {
    const params = toHttpParams({
      page: filter.page,
      pageSize: filter.pageSize,
      search: filter.search ?? undefined,
    });
    return this.http
      .get<ApiResponse<PagedResult<RoleListItemDto>>>(this.rolesBase, { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getById(id: string) {
    return this.http.get<ApiResponse<RoleDto>>(`${this.rolesBase}/${id}`).pipe(map((r) => unwrapApiResponse(r)));
  }

  create(body: CreateRoleRequest) {
    return this.http.post<ApiResponse<RoleDto>>(this.rolesBase, body).pipe(map((r) => unwrapApiResponse(r)));
  }

  update(id: string, body: UpdateRoleRequest) {
    return this.http.put<ApiResponse<RoleDto>>(`${this.rolesBase}/${id}`, body).pipe(map((r) => unwrapApiResponse(r)));
  }

  assignPermissions(id: string, body: AssignRolePermissionsRequest) {
    return this.http
      .put<ApiResponse<RoleDto>>(`${this.rolesBase}/${id}/permissions`, body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getPermissions() {
    return this.http
      .get<ApiResponse<PermissionDto[]>>(this.permissionsBase)
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
