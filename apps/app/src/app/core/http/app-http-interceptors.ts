import type { HttpInterceptorFn } from '@angular/common/http';

import { apiErrorInterceptor } from './api-error.interceptor';
import { authInterceptor } from './auth.interceptor';

export const appHttpInterceptors: HttpInterceptorFn[] = [authInterceptor, apiErrorInterceptor];
