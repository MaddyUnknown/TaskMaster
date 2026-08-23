using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.Library.Common.Auth.Oidc;
using TaskMaster.Library.Common.Configs;
using TaskMaster.Library.Common.Models.Auth;

namespace TaskMaster.Test.UnitTests.CommonTests.AuthTests;

public class AuthTokenProviderTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }

    /// <summary>
    /// Wraps a responder and counts requests across all HttpClient instances,
    /// since AuthTokenProvider creates (and disposes) a client per call.
    /// </summary>
    private sealed class CountingResponder
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public CountingResponder(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public int CallCount;

        public HttpResponseMessage Respond(HttpRequestMessage request)
        {
            Interlocked.Increment(ref CallCount);
            return _responder(request);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private static readonly Func<HttpRequestMessage, HttpResponseMessage> DiscoveryAndToken = request =>
    {
        if (request.RequestUri!.PathAndQuery == "/.well-known/openid-configuration")
        {
            return Json(HttpStatusCode.OK, "{\"token_endpoint\":\"https://idp.example.com/token\"}");
        }

        if (request.RequestUri.PathAndQuery == "/token")
        {
            return Json(HttpStatusCode.OK, "{\"access_token\":\"abc123\",\"expires_in\":3600}");
        }

        return Json(HttpStatusCode.NotFound, "");
    };

    private static AppConfig CreateAppConfig(
        string clientId = "taskmaster-consumer",
        string clientSecret = "secret",
        string scope = "jobs:pull jobs:report")
    {
        return new AppConfig
        {
            ApiBaseUrl = "https://api.example.com",
            Auth = new AuthConfig
            {
                Oidc = new OidcConfig
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    RequestedScope = scope
                }
            }
        };
    }

    private static AuthConfigDetails CreateApiConfig(
        string authority = "https://idp.example.com",
        string audience = "taskmaster-api")
    {
        return new AuthConfigDetails
        {
            Mode = "oidc",
            Oidc = new OidcAuthConfigDetails
            {
                Authority = authority,
                Audience = audience
            }
        };
    }

    private static AuthTokenProvider CreateProvider(
        AppConfig appConfig,
        AuthConfigDetails authConfigDetails,
        Func<HttpRequestMessage, HttpResponseMessage>? responder,
        out Func<int> callCount)
    {
        var counting = new CountingResponder(responder ?? DiscoveryAndToken);
        callCount = () => Volatile.Read(ref counting.CallCount);

        var httpFactory = new Mock<IHttpClientFactory>();
        // A fresh handler/client per CreateClient call: the provider disposes
        // the HttpClient it receives after every use.
        httpFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(new StubHandler(counting.Respond)));

        return new AuthTokenProvider(
            Options.Create(authConfigDetails),
            Options.Create(appConfig),
            httpFactory.Object);
    }

    [Test]
    public async Task GetAccessTokenAsync_ShouldDiscoverTokenEndpointAndAcquireToken()
    {
        var provider = CreateProvider(CreateAppConfig(), CreateApiConfig(), null, out var callCount);

        var token = await provider.GetAccessTokenAsync();

        Assert.That(token, Is.EqualTo("abc123"));
        Assert.That(callCount(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetAccessTokenAsync_TokenRequest_ShouldSendClientCredentialsForm()
    {
        string? form = null;
        var provider = CreateProvider(CreateAppConfig(clientId: "my-client", clientSecret: "my-secret", scope: "jobs:create"), CreateApiConfig(), request =>
        {
            if (request.RequestUri!.PathAndQuery == "/.well-known/openid-configuration")
            {
                return Json(HttpStatusCode.OK, "{\"token_endpoint\":\"https://idp.example.com/token\"}");
            }

            if (request.RequestUri.PathAndQuery == "/token")
            {
                Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(request.Content, Is.Not.Null);
                form = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return Json(HttpStatusCode.OK, "{\"access_token\":\"abc123\",\"expires_in\":3600}");
            }

            return Json(HttpStatusCode.NotFound, "");
        }, out _);

        await provider.GetAccessTokenAsync();

        Assert.That(form, Is.Not.Null);
        var fields = form!.Split('&')
            .Select(p => p.Split('='))
            .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]));
        Assert.Multiple(() =>
        {
            Assert.That(fields["grant_type"], Is.EqualTo("client_credentials"));
            Assert.That(fields["client_id"], Is.EqualTo("my-client"));
            Assert.That(fields["client_secret"], Is.EqualTo("my-secret"));
            Assert.That(fields["scope"], Is.EqualTo("jobs:create"));
            Assert.That(fields["audience"], Is.EqualTo("taskmaster-api"));
        });
    }

    [Test]
    public async Task GetAccessTokenAsync_ShouldCacheTokenAndNotReacquire()
    {
        var provider = CreateProvider(CreateAppConfig(), CreateApiConfig(), null, out var callCount);

        await provider.GetAccessTokenAsync();
        var token = await provider.GetAccessTokenAsync();

        Assert.That(token, Is.EqualTo("abc123"));
        Assert.That(callCount(), Is.EqualTo(2), "Second call should reuse the cached token");
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenCachedTokenExpired_ShouldReacquire()
    {
        var provider = CreateProvider(CreateAppConfig(), CreateApiConfig(), null, out var callCount);

        await provider.GetAccessTokenAsync();
        Assert.That(callCount(), Is.EqualTo(2));

        var expiresAtField = typeof(AuthTokenProvider).GetField(
            "_expiresAtUtc", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        expiresAtField!.SetValue(provider, DateTimeOffset.UtcNow.AddSeconds(-10));

        var token = await provider.GetAccessTokenAsync();

        Assert.That(token, Is.EqualTo("abc123"));
        Assert.That(callCount(), Is.EqualTo(4), "Expired cached token should be reacquired");
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenTokenEndpointReturnsError_ShouldThrow()
    {
        var provider = CreateProvider(CreateAppConfig(), CreateApiConfig(), request =>
        {
            if (request.RequestUri!.PathAndQuery == "/.well-known/openid-configuration")
            {
                return Json(HttpStatusCode.OK, "{\"token_endpoint\":\"https://idp.example.com/token\"}");
            }

            return Json(HttpStatusCode.BadRequest, "{\"error\":\"invalid_client\"}");
        }, out _);

        InvalidOperationException? exception = null;
        try { await provider.GetAccessTokenAsync(); }
        catch (InvalidOperationException ex) { exception = ex; }

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("400"));
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenDiscoveryFails_ShouldThrow()
    {
        var provider = CreateProvider(CreateAppConfig(), CreateApiConfig(), _ => Json(HttpStatusCode.NotFound, ""), out _);

        InvalidOperationException? exception = null;
        try { await provider.GetAccessTokenAsync(); }
        catch (InvalidOperationException ex) { exception = ex; }

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("token endpoint"));
    }

    [Test]
    public async Task GetAccessTokenAsync_WhenTokenResponseHasNoAccessToken_ShouldThrow()
    {
        var provider = CreateProvider(CreateAppConfig(), CreateApiConfig(), request =>
        {
            if (request.RequestUri!.PathAndQuery == "/.well-known/openid-configuration")
            {
                return Json(HttpStatusCode.OK, "{\"token_endpoint\":\"https://idp.example.com/token\"}");
            }

            return Json(HttpStatusCode.OK, "{\"access_token\":null,\"expires_in\":3600}");
        }, out _);

        InvalidOperationException? exception = null;
        try { await provider.GetAccessTokenAsync(); }
        catch (InvalidOperationException ex) { exception = ex; }

        Assert.That(exception, Is.Not.Null);
    }
}
