import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import {
  CreateEmployeeRequest,
  EmployeeDto,
  EmployeeFilterRequest,
  EmployeeListItemDto,
  UpdateEmployeeRequest,
} from '../shared/models/employees.models';

@Injectable({
  providedIn: 'root',
})
export class EmployeesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/employees`;

  getById(id: string): Observable<EmployeeDto> {
    return this.http
      .get<ApiResponse<EmployeeDto>>(`${this.baseUrl}/${id}`)
      .pipe(map((r) => r.data));
  }

  getPagedList(request: EmployeeFilterRequest): Observable<PagedResult<EmployeeListItemDto>> {
    let params = new HttpParams()
      .set('page', request.page.toString())
      .set('pageSize', request.pageSize.toString());

    if (request.search) params = params.set('search', request.search);
    if (request.departmentId) params = params.set('departmentId', request.departmentId);
    if (request.isActive !== undefined && request.isActive !== null)
      params = params.set('isActive', request.isActive.toString());

    return this.http
      .get<ApiResponse<PagedResult<EmployeeListItemDto>>>(this.baseUrl, { params })
      .pipe(map((r) => r.data));
  }

  create(request: CreateEmployeeRequest): Observable<EmployeeDto> {
    return this.http
      .post<ApiResponse<EmployeeDto>>(this.baseUrl, request)
      .pipe(map((r) => r.data));
  }

  update(id: string, request: UpdateEmployeeRequest): Observable<EmployeeDto> {
    return this.http
      .put<ApiResponse<EmployeeDto>>(`${this.baseUrl}/${id}`, request)
      .pipe(map((r) => r.data));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
