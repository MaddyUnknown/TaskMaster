using System.Net.Http.Headers;

namespace TaskMaster.Library.Common.Auth.Oidc
{
    public class BearerTokenDelegatingHandler : DelegatingHandler
    {
        private readonly IAuthTokenProvider _tokenProvider;

        public BearerTokenDelegatingHandler(IAuthTokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}