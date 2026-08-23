export enum AuthMode {
  None = 'none',
  Oidc = 'oidc',
}

export interface OidcConfig {
  authority: string;
  audience: string;
  scopeClaim: string;
}

export interface AuthConfig {
  mode: AuthMode;
  oidc: OidcConfig | null;
}
