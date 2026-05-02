import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import {
  DepartmentDto,
  DepartmentListItemDto,
  DepartmentTreeNodeDto,
  DepartmentFilterRequest,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
} from '../shared/models/department.models';

@Injectable({
  providedIn: 'root',
})
export class DepartmentsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = apiUrl('api/departments');

  getById(id: string): Observable<DepartmentDto> {
    return this.http.get<ApiResponse<DepartmentDto>>(`${this.baseUrl}/${id}`).pipe(map((r) => r.data!));
  }

  getPagedList(request: DepartmentFilterRequest): Observable<PagedResult<DepartmentListItemDto>> {
    const params: any = {
      page: request.page.toString(),
      pageSize: request.pageSize.toString(),
    };

    if (request.search) params.search = request.search;
    if (request.parentDepartmentId) params.parentDepartmentId = request.parentDepartmentId;

    return this.http.get<ApiResponse<PagedResult<DepartmentListItemDto>>>(this.baseUrl, { params }).pipe(map((r) => r.data!));
  }

  getTree(): Observable<DepartmentTreeNodeDto[]> {
    return this.http.get<ApiResponse<DepartmentTreeNodeDto[]>>(`${this.baseUrl}/tree`).pipe(map((r) => r.data!));
  }

  create(request: CreateDepartmentRequest): Observable<DepartmentDto> {
    return this.http.post<ApiResponse<DepartmentDto>>(this.baseUrl, request).pipe(map((r) => r.data!));
  }

  update(id: string, request: UpdateDepartmentRequest): Observable<DepartmentDto> {
    return this.http.put<ApiResponse<DepartmentDto>>(`${this.baseUrl}/${id}`, request).pipe(map((r) => r.data!));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
