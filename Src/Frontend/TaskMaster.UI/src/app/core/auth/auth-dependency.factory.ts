import { AuthDependency } from './auth.dependency';
import { NoAuthDependency } from './no-auth/no-auth.dependency';
import { OidcAuthDependency } from './oidc/oidc-auth.dependency';
import { AuthConfig, AuthMode } from '../models';

export function createAuthDependency(authConfig: AuthConfig): AuthDependency {
  switch (authConfig.mode) {
    case AuthMode.None:
      return new NoAuthDependency();
    case AuthMode.Oidc:
      return new OidcAuthDependency();
    default:
      throw new Error(`Unsupported auth mode: ${authConfig.mode}`);
  }
}
