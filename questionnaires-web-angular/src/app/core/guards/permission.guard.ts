import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

function collectPermissions(route: ActivatedRouteSnapshot): string[] {
  const set = new Set<string>();
  let r: ActivatedRouteSnapshot | null = route;
  while (r) {
    const p = r.data['permissions'] as string[] | undefined;
    if (p?.length) p.forEach((c) => set.add(c));
    r = r.parent;
  }
  return [...set];
}

export const permissionGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const required = collectPermissions(route);
  if (!required.length) return true;
  if (auth.hasAnyPermission(required)) return true;
  void router.navigate(['/403']);
  return false;
};
