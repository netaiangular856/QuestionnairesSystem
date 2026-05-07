import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { readApiErrorsFromBody } from '../../shared/utils/api-helpers';
import { AuthService } from '../auth/auth.service';
import { ErrorDisplayService } from '../services/error-display.service';

export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const errors = inject(ErrorDisplayService);

  return next(req).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse)) {
        return throwError(() => err);
      }

      const loginCall = req.url.includes('/api/auth/login');
      const publicApiCall = req.url.includes('/api/public/');
      if (err.status === 401 && !loginCall && !publicApiCall) {
        auth.logout(false);
        void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      } else if (!loginCall) {
        const apiErrors = readApiErrorsFromBody(err.error);
        if (apiErrors?.length) {
          errors.show(apiErrors.join(' · '));
        } else if (typeof err.error === 'string' && err.error.length > 0) {
          errors.show(err.error);
        } else if (err.message) {
          errors.show(err.message);
        }
      }

      return throwError(() => err);
    }),
  );
};
