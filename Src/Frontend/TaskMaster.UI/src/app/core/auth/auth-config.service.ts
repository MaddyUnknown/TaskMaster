import { Injectable } from '@angular/core';
import { AuthConfig, AuthMode } from '../models';

@Injectable({ providedIn: 'root' })
export class AuthConfigService {
  private config: AuthConfig = { mode: AuthMode.None, oidc: null };

  get current(): AuthConfig {
    return this.config;
  }

  get isAuthEnabled(): boolean {
    return this.config.mode != AuthMode.None;
  }

  load(authConfig: AuthConfig): void {
    this.config = authConfig;
  }
}
