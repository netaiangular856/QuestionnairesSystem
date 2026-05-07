import { HttpErrorResponse, HttpParams } from '@angular/common/http';
import { ApiResponse } from '../models/api.types';

export class ApiBusinessError extends Error {
  constructor(public readonly errors: string[]) {
    super(errors.join(' | '));
  }
}

/** Reads `errors` from a failed API JSON body (e.g. 400/409) regardless of `success` / `succeeded`. */
export function readApiErrorsFromBody(body: unknown): string[] | null {
  if (typeof body !== 'object' || body === null) return null;
  const errors = (body as { errors?: unknown }).errors;
  if (!Array.isArray(errors) || errors.length === 0) return null;
  const parts = errors.filter((e): e is string => typeof e === 'string' && e.trim().length > 0);
  return parts.length > 0 ? parts : null;
}

/** User-facing message from HttpErrorResponse, ApiBusinessError, or generic Error. */
export function extractApiErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof ApiBusinessError && err.errors.length > 0) {
    return err.errors.join(' · ');
  }
  if (err instanceof HttpErrorResponse) {
    const fromBody = readApiErrorsFromBody(err.error);
    if (fromBody) return fromBody.join(' · ');
    if (typeof err.error === 'string' && err.error.trim()) return err.error.trim();
    if (typeof err.error === 'object' && err.error !== null) {
      const body = err.error as { message?: unknown; detail?: unknown; title?: unknown; error?: unknown };
      const candidate =
        (typeof body.message === 'string' && body.message) ||
        (typeof body.detail === 'string' && body.detail) ||
        (typeof body.title === 'string' && body.title) ||
        (typeof body.error === 'string' && body.error) ||
        '';
      if (candidate && candidate.trim()) return candidate.trim();
    }
    if (err.status === 0) return fallback;
    if (typeof err.statusText === 'string' && err.statusText.trim() && err.statusText !== 'OK') {
      return err.status > 0 ? `${err.statusText} (${err.status})` : err.statusText;
    }
    if (typeof err.message === 'string' && err.message.trim()) return err.message;
  }
  if (err instanceof Error && err.message) return err.message;
  return fallback;
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
