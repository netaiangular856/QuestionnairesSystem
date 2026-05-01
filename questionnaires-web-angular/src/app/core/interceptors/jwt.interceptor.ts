import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { isSameAppApiRequest } from '../config/api-url';
import { AuthService } from '../auth/auth.service';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.accessToken();
  if (token && isSameAppApiRequest(req.url)) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  return next(req);
};
