using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using TaskMaster.Library.Common.Configs;
using TaskMaster.Library.Common.Constants;
using TaskMaster.Library.Common.Models.Auth;

namespace TaskMaster.Library.Common.Auth.Oidc
{
    public class AuthTokenProvider : IAuthTokenProvider
    {
        private const string OidcDiscoveryPath = "/.well-known/openid-configuration";
        private static readonly TimeSpan RefreshSlack = TimeSpan.FromSeconds(60);

        private readonly IOptions<AuthConfigDetails> _authConfig;
        private readonly IOptions<AppConfig> _appConfig;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1); // Cannot call lock in async context

        private string? _cachedToken;
        private DateTimeOffset _expiresAtUtc = DateTimeOffset.MinValue;

        public AuthTokenProvider(
            IOptions<AuthConfigDetails> authConfig,
            IOptions<AppConfig> appConfig,
            IHttpClientFactory httpClientFactory)
        {
            _authConfig = authConfig;
            _appConfig = appConfig;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(_cachedToken) && _expiresAtUtc > DateTimeOffset.UtcNow.Add(RefreshSlack))
            {
                return _cachedToken;
            }

            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrEmpty(_cachedToken) && _expiresAtUtc > DateTimeOffset.UtcNow.Add(RefreshSlack))
                {
                    return _cachedToken;
                }

                var token = await AcquireTokenAsync(cancellationToken);
                if (token.HasValue)
                {
                    _cachedToken = token.Value.AccessToken;
                    _expiresAtUtc = token.Value.ExpiresAtUtc;
                }

                return _cachedToken;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task<(string AccessToken, DateTimeOffset ExpiresAtUtc)?> AcquireTokenAsync(CancellationToken cancellationToken)
        {
            var authority = ResolveAuthority();
            var tokenEndpoint = await DiscoverTokenEndpointAsync(authority, cancellationToken);

            if (string.IsNullOrWhiteSpace(tokenEndpoint))
            {
                throw new InvalidOperationException(ErrorMessage.AuthTokenEndpointNotFound(authority));
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _appConfig.Value.Auth.Oidc?.ClientId ?? string.Empty,
                    ["client_secret"] = _appConfig.Value.Auth.Oidc?.ClientSecret ?? string.Empty,
                    ["scope"] = _appConfig.Value.Auth.Oidc?.RequestedScope ?? string.Empty,
                    ["audience"] = _authConfig.Value.Oidc?.Audience ?? string.Empty,
                })
            };

            using var httpClient = _httpClientFactory.CreateClient("public");
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(ErrorMessage.AuthTokenRequestFailed((int)response.StatusCode));
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
            if (tokenResponse?.AccessToken == null)
            {
                throw new InvalidOperationException(ErrorMessage.AuthTokenRequestFailed((int)response.StatusCode));
            }

            var expiresIn = tokenResponse.ExpiresIn > 0 ? tokenResponse.ExpiresIn : 3600;
            return (tokenResponse.AccessToken, DateTimeOffset.UtcNow.AddSeconds(expiresIn));
        }

        private string ResolveAuthority()
        {
            var configuredAuthority = _authConfig.Value.Oidc?.Authority;
            if (!string.IsNullOrWhiteSpace(configuredAuthority))
            {
                return configuredAuthority.TrimEnd('/');
            }

            return configuredAuthority ?? string.Empty;
        }

        private async Task<string> DiscoverTokenEndpointAsync(string authority, CancellationToken cancellationToken)
        {
            var discoveryUri = new Uri($"{authority}{OidcDiscoveryPath}");

            using var httpClient = _httpClientFactory.CreateClient("public");
            using var response = await httpClient.GetAsync(discoveryUri, cancellationToken);
            if (!response.IsSuccessStatusCode) return string.Empty;

            var body = await response.Content.ReadFromJsonAsync<DiscoveryDocument>(cancellationToken);
            return body?.TokenEndpoint ?? string.Empty;
        }

        private class DiscoveryDocument
        {
            [System.Text.Json.Serialization.JsonPropertyName("token_endpoint")]
            public string? TokenEndpoint { get; set; }
        }

        private class TokenResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
            public long ExpiresIn { get; set; }
        }
    }
}