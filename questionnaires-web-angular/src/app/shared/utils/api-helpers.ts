import { HttpParams } from '@angular/common/http';
import { ApiResponse } from '../models/api.types';

export class ApiBusinessError extends Error {
  constructor(public readonly errors: string[]) {
    super(errors.join(' | '));
  }
}

function isApiSuccess(response: ApiResponse<unknown>): boolean {
  return response.success ?? response.succeeded ?? false;
}

export function unwrapApiResponse<T>(response: ApiResponse<T>): T {
  if (isApiSuccess(response)) return response.data;
  throw new ApiBusinessError(response.errors ?? ['Unknown API error']);
}

export function unwrapApiVoid(response: ApiResponse<unknown>): void {
  if (isApiSuccess(response)) return;
  throw new ApiBusinessError(response.errors ?? ['Unknown API error']);
}

export function toHttpParams(record: Record<string, string | number | boolean | null | undefined>): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(record)) {
    if (value === undefined || value === null || value === '') continue;
    params = params.set(key, String(value));
  }
  return params;
}
