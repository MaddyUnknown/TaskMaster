import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AUTH_SERVICE } from './auth.service';
import { AuthState } from './auth.models';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AUTH_SERVICE);
  const router = inject(Router);

  switch (authService.currentAuthState) {
    case AuthState.authenticated:
      return true;

    case AuthState.unauthenticated:
      authService.login();
      return false;

    case AuthState.error:
      return router.createUrlTree(['/auth-error']);

    case AuthState.initializing:
      return false;
  }
};
