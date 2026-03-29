import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { API_URL } from '../config/api-url.token';
import { applyApiAuthHeader } from './apply-api-auth-header';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const apiUrl = inject(API_URL);

  return from(authService.getToken()).pipe(
    switchMap((token) => next(applyApiAuthHeader(request, token, apiUrl))),
  );
};
