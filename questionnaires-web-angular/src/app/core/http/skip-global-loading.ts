import { HttpHeaders } from '@angular/common/http';

/** Tells {@link loadingInterceptor} not to bump the app-shell overlay (dropdowns, AI, etc.). */
export const skipGlobalLoadingHttpOptions = {
  headers: new HttpHeaders({ 'X-Skip-Loading': '1' }),
};
