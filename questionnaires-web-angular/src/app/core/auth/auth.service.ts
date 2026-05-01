import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { map, tap } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse } from '../../shared/models/api.types';
import { ChangePasswordRequest, LoginRequest, LoginResponseDto, RegisterRequest } from '../../shared/models/employee.models';
import { unwrapApiResponse, unwrapApiVoid } from '../../shared/utils/api-helpers';
import {
  decodeJwtPayload,
  readEmailFromPayload,
  readPermissionCodesFromPayload,
  readSubFromPayload,
  readUserNameFromPayload,
} from './jwt.util';

const LS_TOKEN = 'questionnaires_os_access_token';
const LS_EXPIRES = 'questionnaires_os_expires_at_utc';

export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
  permissions: readonly string[];
  userId: string | null;
  email: string | null;
  userName: string | null;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<AuthSession | null>(null);

  readonly sessionSnapshot = computed(() => this.session());

  constructor() {
    this.restoreFromStorage();
  }

  isAuthenticated(): boolean {
    const s = this.session();
    if (!s) return false;
    return !this.isExpired(s.expiresAtUtc);
  }

  accessToken(): string | null {
    return this.session()?.accessToken ?? null;
  }

  hasPermission(code: string): boolean {
    const perms = this.session()?.permissions ?? [];
    return perms.includes(code);
  }

  hasAnyPermission(codes: readonly string[]): boolean {
    if (codes.length === 0) return true;
    return codes.some((c) => this.hasPermission(c));
  }

  login(body: LoginRequest) {
    return this.http
      .post<ApiResponse<LoginResponseDto>>(apiUrl('/api/auth/login'), body)
      .pipe(
        map((r) => unwrapApiResponse(r)),
        tap((dto) => this.persistSession(dto)),
      );
  }

  register(body: RegisterRequest) {
    return this.http
      .post<ApiResponse<LoginResponseDto>>(apiUrl('/api/auth/register'), body)
      .pipe(
        map((r) => unwrapApiResponse(r)),
        tap((dto) => this.persistSession(dto)),
      );
  }

  changePassword(body: ChangePasswordRequest) {
    return this.http
      .post<ApiResponse<unknown>>(apiUrl('/api/auth/change-password'), body)
      .pipe(map((r) => unwrapApiVoid(r)));
  }

  logout(navigate = true): void {
    this.clearStorage();
    this.session.set(null);
    if (navigate) void this.router.navigate(['/login']);
  }

  restoreFromStorage(): void {
    const token = localStorage.getItem(LS_TOKEN);
    const exp = localStorage.getItem(LS_EXPIRES);
    if (!token || !exp) return;
    if (this.isExpired(exp)) {
      this.clearStorage();
      return;
    }
    this.session.set(this.mapTokenToSession(token, exp));
  }

  private persistSession(dto: LoginResponseDto): void {
    localStorage.setItem(LS_TOKEN, dto.accessToken);
    localStorage.setItem(LS_EXPIRES, dto.expiresAtUtc);
    this.session.set(this.mapTokenToSession(dto.accessToken, dto.expiresAtUtc));
  }

  private mapTokenToSession(accessToken: string, expiresAtUtc: string): AuthSession {
    const payload = decodeJwtPayload(accessToken);
    return {
      accessToken,
      expiresAtUtc,
      permissions: readPermissionCodesFromPayload(payload),
      userId: readSubFromPayload(payload),
      email: readEmailFromPayload(payload),
      userName: readUserNameFromPayload(payload),
    };
  }

  private clearStorage(): void {
    localStorage.removeItem(LS_TOKEN);
    localStorage.removeItem(LS_EXPIRES);
  }

  private isExpired(expiresAtUtc: string): boolean {
    const t = Date.parse(expiresAtUtc);
    if (Number.isNaN(t)) return true;
    return t <= Date.now();
  }
}

