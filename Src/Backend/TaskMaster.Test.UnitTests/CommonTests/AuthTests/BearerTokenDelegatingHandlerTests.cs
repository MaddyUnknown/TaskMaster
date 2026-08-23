using Moq;
using System.Net;
using TaskMaster.Library.Common.Auth.Oidc;

namespace TaskMaster.Test.UnitTests.CommonTests.AuthTests;

public class BearerTokenDelegatingHandlerTests
{
    private Mock<IAuthTokenProvider> _authTokenProvider = null!;

    [SetUp]
    public void SetupMock()
    {
        _authTokenProvider = new(MockBehavior.Strict);
    }

    [Test]
    public async Task SendAsync_WhenTokenAvailable_ShouldAttachBearerHeader()
    {
        // Arrange
        _authTokenProvider.Setup(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync("test-token");

        var inner = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var handler = new BearerTokenDelegatingHandler(_authTokenProvider.Object)
        {
            InnerHandler = inner
        };

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test/jobs");

        // Act
        await client.SendAsync(request);
        
        // Assert
        Assert.That(inner.Request?.Headers.Authorization?.ToString(), Is.EqualTo("Bearer test-token"));
    }

    [Test]
    public async Task SendAsync_WhenTokenUnavailable_ShouldNotAttachBearerHeader()
    {
        // Arrange
        _authTokenProvider.Setup(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(string.Empty);
        var inner = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var handler = new BearerTokenDelegatingHandler(_authTokenProvider.Object)
        {
            InnerHandler = inner
        };

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test/jobs");

        // Act
        await client.SendAsync(request);

        // Assert
        Assert.That(inner.Request?.Headers.Authorization, Is.Null);
    }

    [Test]
    public void SendAsync_WhenIdpCallFails_ShouldThrowException()
    {
        // Arrange
        _authTokenProvider.Setup(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>())).Throws<InvalidOperationException>();

        var handler = new BearerTokenDelegatingHandler(_authTokenProvider.Object)
        {
            InnerHandler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))
        };

        using var client = new HttpClient(handler);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test/jobs");

        // Act + Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _response;

        public HttpRequestMessage? Request { get; private set; }

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(_response(request));
        }
    }
}