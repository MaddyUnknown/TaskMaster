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

  get apiEndpoint(): string {
    const baseUrl = this.config.apiBaseUrl;
    if (/^https?:\/\//i.test(baseUrl)) {
      return baseUrl;
    }
    return new URL(baseUrl, window.location.origin).href;
  }

  load(appConfig: AppConfig): void {
    this.config = { ...this.config, ...appConfig };
  }
}
