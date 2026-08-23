import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { AppConfig, AuthConfig } from './app/core/models';
import '@lottiefiles/dotlottie-wc';

function loadAppConfig(): Promise<AppConfig> {
  return fetch('/app-config.json').then((res) => res.json());
}

function loadAuthConfig(apiBaseUrl: string): Promise<AuthConfig> {
  return fetch(`${apiBaseUrl}/auth/config`)
    .then((res) => res.json())
    .then((res) => res.data);
}

async function main() {
  // load authentication config
  const config = await loadAppConfig();
  const authConfig = await loadAuthConfig(config.apiBaseUrl);
  await bootstrapApplication(AppComponent, appConfig(config, authConfig)).catch(
    (err) => console.error(err),
  );
}

main();
