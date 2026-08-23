import { HttpInterceptorFn } from '@angular/common/http';
import { EnvironmentProviders, Provider } from '@angular/core';

export interface AuthDependency {
  getHttpInterceptors(): HttpInterceptorFn[];
  getProviders(): (Provider | EnvironmentProviders)[];
}
