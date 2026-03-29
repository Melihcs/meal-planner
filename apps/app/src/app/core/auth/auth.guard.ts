import { inject } from '@angular/core';
import type { CanActivateChildFn, CanMatchFn } from '@angular/router';
import { Router } from '@angular/router';

import { AuthService } from './auth.service';

export const authCanMatchGuard: CanMatchFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return (await authService.isAuthenticated()) ? true : router.createUrlTree(['/auth/login']);
};

export const authCanActivateChildGuard: CanActivateChildFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return (await authService.isAuthenticated()) ? true : router.createUrlTree(['/auth/login']);
};

export const guestOnlyCanMatchGuard: CanMatchFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return (await authService.isAuthenticated()) ? router.createUrlTree(['/tabs/recipes']) : true;
};
