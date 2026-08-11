using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TaskMaster.API.Interfaces.EventHandler;
using TaskMaster.API.Interfaces.Publisher;
using TaskMaster.API.Publishers;

namespace TaskMaster.Test.UnitTests.APITests;

public class EventPublisherTests
{
    private Mock<IServiceScopeFactory> _scopeFactory = null!;
    private Mock<IServiceScope> _scope = null!;
    private Mock<IServiceProvider> _serviceProvider = null!;
    private Mock<ILogger<EventPublisher>> _logger = null!;

    private class TestEvent { }

    [SetUp]
    public void Setup()
    {
        _serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        _serviceProvider
            .Setup(sp => sp.GetService(typeof(IEnumerable<IEventHandler<TestEvent>>)))
            .Returns(Array.Empty<IEventHandler<TestEvent>>());

        _scope = new Mock<IServiceScope>(MockBehavior.Strict);
        _scope.Setup(s => s.ServiceProvider).Returns(_serviceProvider.Object);
        _scope.Setup(s => s.Dispose());

        _scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        _scopeFactory.Setup(f => f.CreateScope()).Returns(_scope.Object);

        _logger = new Mock<ILogger<EventPublisher>>();
    }

    private IEventPublisher CreatePublisher() => new EventPublisher(_scopeFactory.Object, _logger.Object);

    private void SetupHandlers(params IEventHandler<TestEvent>[] handlers)
    {
        _serviceProvider
            .Setup(sp => sp.GetService(typeof(IEnumerable<IEventHandler<TestEvent>>)))
            .Returns(handlers);
    }

    [Test]
    public async Task PublishAsync_WhenMultipleHandlers_ShouldResolveHandlersFromNewScopeAndInvokeInOrder()
    {
        // Arrange
        var invocationOrder = new List<string>();
        var handlers = new IEventHandler<TestEvent>[]
        {
            new RecordingHandler("first", 2, invocationOrder),
            new RecordingHandler("second", 1, invocationOrder),
            new RecordingHandler("third", 0, invocationOrder)
        };
        SetupHandlers(handlers);

        // Act
        await CreatePublisher().PublishAsync(new TestEvent());

        // Assert
        Assert.That(invocationOrder, Is.EqualTo(new[] { "third", "second", "first" }));
        _scopeFactory.Verify(f => f.CreateScope(), Times.Once);
        _scope.Verify(s => s.Dispose(), Times.Once);
    }

    [Test]
    public async Task PublishAsync_WhenHandlerThrows_ShouldNotPropagateAndShouldContinueToNextHandler()
    {
        // Arrange
        var invocationOrder = new List<string>();
        var handlers = new IEventHandler<TestEvent>[]
        {
            new ThrowingHandler(invocationOrder),
            new RecordingHandler("after", 1, invocationOrder)
        };
        SetupHandlers(handlers);

        // Act
        await CreatePublisher().PublishAsync(new TestEvent());

        // Assert
        Assert.That(invocationOrder, Is.EqualTo(new[] { "after" }));
    }

    [Test]
    public async Task PublishAsync_WhenNoHandlers_ShouldCompleteWithoutError()
    {
        // Act + Assert
        await CreatePublisher().PublishAsync(new TestEvent());
    }

    [Test]
    public async Task PublishAsync_WhenSingleHandler_ShouldInvokeHandler()
    {
        // Arrange
        var invocationOrder = new List<string>();
        SetupHandlers(new RecordingHandler("only", 0, invocationOrder));

        // Act
        await CreatePublisher().PublishAsync(new TestEvent());

        // Assert
        Assert.That(invocationOrder, Is.EqualTo(new[] { "only" }));
    }

    private sealed class RecordingHandler : IEventHandler<TestEvent>
    {
        private readonly string _name;
        private readonly int _order;
        private readonly List<string> _log;

        public RecordingHandler(string name, int order, List<string> log)
        {
            _name = name;
            _order = order;
            _log = log;
        }

        public int Order => _order;

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            _log.Add(_name);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IEventHandler<TestEvent>
    {
        private readonly List<string> _log;

        public ThrowingHandler(List<string> log)
        {
            _log = log;
        }

        public int Order => 0;

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
