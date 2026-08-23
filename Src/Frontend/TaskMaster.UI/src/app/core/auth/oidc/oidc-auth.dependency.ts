import { HttpInterceptorFn } from '@angular/common/http';
import { AuthDependency } from '../auth.dependency';
import { EnvironmentProviders, Provider } from '@angular/core';
import {
  authInterceptor,
  provideAuth,
  StsConfigLoader,
} from 'angular-auth-oidc-client';
import { AuthConfigLoader } from './oidc-auth-config.loader';
import { AUTH_SERVICE } from '../auth.service';
import { OidcAuthService } from './oidc-auth.service';

export class OidcAuthDependency implements AuthDependency {
  getHttpInterceptors(): HttpInterceptorFn[] {
    return [authInterceptor()];
  }

  getProviders(): (Provider | EnvironmentProviders)[] {
    return [
      provideAuth({
        loader: {
          provide: StsConfigLoader,
          useClass: AuthConfigLoader,
        },
      }),
      {
        provide: AUTH_SERVICE,
        useClass: OidcAuthService,
      },
    ];
  }
}
