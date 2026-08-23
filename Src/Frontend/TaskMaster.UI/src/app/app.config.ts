import {
  ApplicationConfig,
  provideZoneChangeDetection,
  APP_INITIALIZER,
  inject,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import {
  provideHttpClient,
  withFetch,
  withInterceptors,
} from '@angular/common/http';

import { getRoutes } from './app.routes';
import { AppConfigService } from './core/services/app-config.service';
import { AuthConfigService } from './core/auth/auth-config.service';
import { AuthConfig } from './core/models';
import { AppConfig } from './core/models';
import { createAuthDependency } from './core/auth/auth-dependency.factory';
import { AUTH_SERVICE } from './core/auth/auth.service';

export function appConfig(
  appConfig: AppConfig,
  authConfig: AuthConfig,
): ApplicationConfig {
  const authDependency = createAuthDependency(authConfig);

  return {
    providers: [
      provideZoneChangeDetection({ eventCoalescing: true }),
      provideRouter(getRoutes(authConfig)),
      provideAnimations(),
      provideHttpClient(
        withFetch(),
        withInterceptors(authDependency.getHttpInterceptors()),
      ),
      ...authDependency.getProviders(),
      {
        provide: APP_INITIALIZER,
        useFactory: () => {
          const appConfigService = inject(AppConfigService);
          const authConfigService = inject(AuthConfigService);
          const auth = inject(AUTH_SERVICE);

          return () => {
            appConfigService.load(appConfig);
            authConfigService.load(authConfig);

            return auth.init();
          };
        },
        multi: true,
      },
    ],
  };
}
