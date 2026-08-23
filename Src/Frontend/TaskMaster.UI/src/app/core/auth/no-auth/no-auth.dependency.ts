import { HttpInterceptorFn } from '@angular/common/http';
import { EnvironmentProviders, Provider } from '@angular/core';
import { AuthDependency } from '../auth.dependency';
import { NoAuthService } from './no-auth.service';
import { AUTH_SERVICE } from '../auth.service';

export class NoAuthDependency implements AuthDependency {
  getHttpInterceptors(): HttpInterceptorFn[] {
    return [];
  }

  getProviders(): (Provider | EnvironmentProviders)[] {
    return [
      {
        provide: AUTH_SERVICE,
        useClass: NoAuthService,
      },
    ];
  }
}
