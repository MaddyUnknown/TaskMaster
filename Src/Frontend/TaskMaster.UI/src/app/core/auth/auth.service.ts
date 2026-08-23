import { Observable } from 'rxjs';
import { AuthState, UserPermission } from './auth.models';
import { InjectionToken } from '@angular/core';

export const AUTH_SERVICE = new InjectionToken<AuthService>('AUTH_SERVICE');

export interface AuthService {
  get authState$(): Observable<AuthState>;
  get isAuthenticated$(): Observable<boolean>;
  get permissions$(): Observable<UserPermission[]>;

  get lastAuthError(): string;
  get currentAuthState(): AuthState;
  get isAuthenticated(): boolean;
  get currentPermission(): UserPermission[];

  init(): Observable<AuthState>;
  login(): void;
  logout(): void;
  hasPermission(permission: UserPermission): boolean;
}
