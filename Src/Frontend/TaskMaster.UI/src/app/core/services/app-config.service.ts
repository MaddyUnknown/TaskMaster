import { Injectable } from '@angular/core';
import { AppConfig } from '../models';

@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private config: AppConfig = {
    clientId: '',
    redirectUri: '',
    postLogoutRedirectUri: '',
    apiBaseUrl: '',
  };

  get current(): AppConfig {
    return this.config;
  }

  load(appConfig: AppConfig): void {
    this.config = { ...this.config, ...appConfig };
  }
}
