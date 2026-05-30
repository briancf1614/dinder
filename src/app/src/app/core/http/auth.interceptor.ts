import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { catchError, switchMap, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const authService = inject(AuthService);
  const token = authService.getAccessToken();

  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // If 401, attempt token refresh once
      if (error.status === 401 && !req.url.includes('/refresh')) {
        return authService.refreshToken().pipe(
          switchMap(newTokens => {
            const retryReq = req.clone({
              setHeaders: { Authorization: `Bearer ${newTokens.accessToken}` }
            });
            return next(retryReq);
          }),
          catchError(refreshError => {
            authService.logout();
            return throwError(() => refreshError);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
