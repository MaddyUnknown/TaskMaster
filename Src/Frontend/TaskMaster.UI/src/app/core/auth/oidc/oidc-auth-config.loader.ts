import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import {
  LogLevel,
  OpenIdConfiguration,
  StsConfigLoader,
} from 'angular-auth-oidc-client';
import { AppConfigService } from '../../services/app-config.service';
import { AuthConfigService } from '../auth-config.service';
import { AuthMode } from '../../models';
import { SCOPE_PERMISSION_MAP } from './oidc-scope-permission.map';

@Injectable()
export class AuthConfigLoader implements StsConfigLoader {
  constructor(
    private authConfigService: AuthConfigService,
    private appConfigService: AppConfigService,
  ) {}

  loadConfigs(): Observable<OpenIdConfiguration[]> {
    if (this.authConfigService.current?.mode != AuthMode.Oidc) return of([]);

    const server = this.authConfigService.current.oidc!;
    const app = this.appConfigService.current;
    const scope = `openid offline_access ${Object.keys(SCOPE_PERMISSION_MAP).join(' ')}`;

    const config: OpenIdConfiguration = {
      logLevel: LogLevel.Error,
      clientId: app.clientId,
      redirectUrl: app.redirectUri,
      postLogoutRedirectUri: app.postLogoutRedirectUri,
      authority: server.authority,
      scope: scope,
      responseType: 'code',
      silentRenew: true,
      useRefreshToken: true,
      triggerRefreshWhenIdTokenExpired: true,
      secureRoutes: [this.appConfigService.current.apiBaseUrl],
      customParamsAuthRequest: server.audience
        ? { audience: server.audience }
        : undefined,
    };

    return of([config]);
  }
}
