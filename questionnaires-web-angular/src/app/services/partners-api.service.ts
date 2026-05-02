import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../shared/models/api.types';
import {
  CreatePartnerRequest,
  PartnerDto,
  PartnerFilterRequest,
  PartnerListItemDto,
  UpdatePartnerRequest,
} from '../shared/models/partners.models';

@Injectable({
  providedIn: 'root',
})
export class PartnersApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/partners`;

  getById(id: string): Observable<PartnerDto> {
    return this.http
      .get<ApiResponse<PartnerDto>>(`${this.baseUrl}/${id}`)
      .pipe(map((r) => r.data));
  }

  getPagedList(request: PartnerFilterRequest): Observable<PagedResult<PartnerListItemDto>> {
    let params = new HttpParams()
      .set('page', request.page.toString())
      .set('pageSize', request.pageSize.toString());

    if (request.search) params = params.set('search', request.search);
    if (request.type) params = params.set('type', request.type.toString());
    if (request.isActive !== undefined && request.isActive !== null)
      params = params.set('isActive', request.isActive.toString());

    return this.http
      .get<ApiResponse<PagedResult<PartnerListItemDto>>>(this.baseUrl, { params })
      .pipe(map((r) => r.data));
  }

  create(request: CreatePartnerRequest): Observable<PartnerDto> {
    return this.http
      .post<ApiResponse<PartnerDto>>(this.baseUrl, request)
      .pipe(map((r) => r.data));
  }

  update(id: string, request: UpdatePartnerRequest): Observable<PartnerDto> {
    return this.http
      .put<ApiResponse<PartnerDto>>(`${this.baseUrl}/${id}`, request)
      .pipe(map((r) => r.data));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
