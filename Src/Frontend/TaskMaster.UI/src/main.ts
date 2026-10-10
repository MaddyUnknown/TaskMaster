import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { AppConfig, AuthConfig, SystemInfo } from './app/core/models';
import '@lottiefiles/dotlottie-wc';

function loadAppConfig(): Promise<AppConfig> {
  return fetch('/app-config.json').then((res) => res.json());
}

function loadAuthConfig(apiBaseUrl: string): Promise<AuthConfig> {
  return fetch(`${apiBaseUrl}/auth/config`)
    .then((res) => res.json())
    .then((res) => res.data);
}

function loadSystemInfo(apiBaseUrl: string): Promise<SystemInfo> {
  return fetch(`${apiBaseUrl}/system/info`)
    .then((res) => res.json())
    .then((res) => res.data)
    .catch(() => ({ version: '—', environment: '—' }));
}

async function main() {
  // load authentication config
  const config = await loadAppConfig();
  const [authConfig, systemInfo] = await Promise.all([
    loadAuthConfig(config.apiBaseUrl),
    loadSystemInfo(config.apiBaseUrl),
  ]);
  await bootstrapApplication(
    AppComponent,
    appConfig(config, authConfig, systemInfo),
  ).catch((err) => console.error(err));
}

main();
