import { HttpErrorResponse, type HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { ToastService } from '../ui/toast.service';

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const toastService = inject(ToastService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      switch (error.status) {
        case 401:
          void authService.signOut();
          void router.navigateByUrl('/auth/login', { replaceUrl: true });
          break;
        case 403:
          void toastService.showError('Access denied');
          break;
        case 422:
          break;
        default:
          if (error.status >= 500) {
            void toastService.showError('Something went wrong');
          }
          break;
      }

      return throwError(() => error);
    }),
  );
};
