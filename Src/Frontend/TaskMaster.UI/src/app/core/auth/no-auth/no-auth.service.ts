import { BehaviorSubject, map, Observable, of } from 'rxjs';
import { AuthService } from '../auth.service';
import { AuthConfigService } from '../auth-config.service';
import { AuthMode } from '../../models';
import { AuthState, UserPermission } from '../auth.models';

export class NoAuthService implements AuthService {
  private readonly permissionSubject$ = new BehaviorSubject<UserPermission[]>(
    [],
  );
  private readonly authStateSubject$ = new BehaviorSubject<AuthState>(
    AuthState.initializing,
  );

  constructor(private authConfigService: AuthConfigService) {}

  get lastAuthError(): string {
    return '';
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
    if (this.authConfigService.current.mode != AuthMode.None) {
      throw new Error(`Auth Mode not 'none'`);
    }

    this.authStateSubject$.next(AuthState.authenticated);

    this.refreshPermission();

    return of(this.authStateSubject$.value);
  }

  login(): void {}

  logout(): void {}

  hasPermission(permission: string): boolean {
    return true;
  }

  private refreshPermission(): void {
    this.permissionSubject$.next(Object.values(UserPermission));
  }
}
