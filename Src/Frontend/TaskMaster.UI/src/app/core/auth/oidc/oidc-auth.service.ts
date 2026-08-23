import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable, of, switchMap } from 'rxjs';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { AuthConfigService } from '../auth-config.service';
import { AuthService } from '../auth.service';
import { AuthState, UserPermission } from '../auth.models';
import { AuthMode } from '../../models';
import { SCOPE_PERMISSION_MAP } from './oidc-scope-permission.map';

@Injectable()
export class OidcAuthService implements AuthService {
  private lastCheckAuthErrorValue = '';

  private readonly permissionSubject$ = new BehaviorSubject<UserPermission[]>(
    [],
  );

  private readonly authStateSubject$ = new BehaviorSubject<AuthState>(
    AuthState.initializing,
  );

  constructor(
    private readonly oidc: OidcSecurityService,
    private readonly authConfigService: AuthConfigService,
  ) {}

  get lastAuthError(): string {
    return this.lastCheckAuthErrorValue;
  }

  get authState$(): Observable<AuthState> {
    return this.authStateSubject$.asObservable();
  }

  get currentAuthState(): AuthState {
    return this.authStateSubject$.value;
  }

  get isAuthenticated(): boolean {
    return this.authStateSubject$.value === AuthState.authenticated;
  }

  get isAuthenticated$(): Observable<boolean> {
    return this.authStateSubject$
      .asObservable()
      .pipe(map((state) => state === AuthState.authenticated));
  }

  get permissions$(): Observable<UserPermission[]> {
    return this.permissionSubject$.asObservable();
  }

  get currentPermission(): UserPermission[] {
    return this.permissionSubject$.value;
  }

  init(): Observable<AuthState> {
    if (this.authConfigService.current.mode !== AuthMode.Oidc) {
      throw new Error(`Auth Mode not 'oidc'`);
    }

    return this.oidc.checkAuth().pipe(
      switchMap((res) => {
        this.lastCheckAuthErrorValue = res.errorMessage ?? '';

        if (res.errorMessage) {
          this.authStateSubject$.next(AuthState.error);
          return of(AuthState.error);
        }

        if (!res.isAuthenticated) {
          this.authStateSubject$.next(AuthState.unauthenticated);
          return of(AuthState.unauthenticated);
        }

        return this.refreshScopes().pipe(
          map(() => {
            this.authStateSubject$.next(AuthState.authenticated);

            return AuthState.authenticated;
          }),
        );
      }),
    );
  }

  login(): void {
    this.oidc.authorize();
  }

  logout(): void {
    this.oidc.logoff().subscribe();
  }

  hasPermission(permission: UserPermission): boolean {
    return this.permissionSubject$.value.includes(permission);
  }

  private refreshScopes(): Observable<boolean> {
    if (
      this.authStateSubject$.value !== AuthState.authenticated &&
      this.authStateSubject$.value !== AuthState.initializing
    ) {
      return of(false);
    }

    return this.oidc.getAccessToken().pipe(
      map((token) => {
        const scopes = this.parseScopes(token);

        const permissions = scopes
          .map((scope) => SCOPE_PERMISSION_MAP[scope])
          .filter(
            (permission): permission is UserPermission =>
              permission !== undefined,
          );

        this.permissionSubject$.next(permissions);

        return true;
      }),
    );
  }

  private parseScopes(token: string): string[] {
    if (!token) {
      return [];
    }

    const scopeName =
      this.authConfigService.current.oidc?.scopeClaim ?? 'scope';

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const raw = payload[scopeName];

      if (!raw) {
        return [];
      }

      return typeof raw === 'string' ? raw.split(/\s+/).filter(Boolean) : raw;
    } catch {
      return [];
    }
  }
}
