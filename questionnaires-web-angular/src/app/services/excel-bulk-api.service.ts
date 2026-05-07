import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from '../core/config/api-url';
import { ApiResponse } from '../shared/models/api.types';

export type ExcelTemplateScopeParam =
  | 'all'
  | 'departments'
  | 'employees'
  | 'partners'
  | 'users'
  | 'templates'
  | 'surveys'
  | 'recommendations'
  | 'actionPlans'
  | 'initiatives';

export interface ExcelImportRowErrorDto {
  sheet: string;
  rowNumber: number;
  message: string;
}

export interface ExcelImportResultDto {
  departmentsImported: number;
  employeesImported: number;
  partnersImported: number;
  usersImported: number;
  templatesImported: number;
  surveysImported: number;
  responsesImported: number;
  recommendationsImported: number;
  actionPlansImported: number;
  initiativesImported: number;
  errors: ExcelImportRowErrorDto[];
}

@Injectable({ providedIn: 'root' })
export class ExcelBulkApiService {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl('/api/excel-bulk');

  /** scope API values match backend ExcelTemplateScope enum names (lowercase). */
  downloadTemplate(scope: ExcelTemplateScopeParam, samples: boolean): Observable<Blob> {
    return this.http.get(`${this.base}/template`, {
      params: {
        scope,
        samples: String(samples),
      },
      responseType: 'blob',
    });
  }

  import(file: File): Observable<ApiResponse<ExcelImportResultDto>> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<ApiResponse<ExcelImportResultDto>>(`${this.base}/import`, fd);
  }
}
