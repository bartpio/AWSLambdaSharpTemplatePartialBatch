using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using Kralizek.Lambda;
using Kralizek.Lambda.PartialBatch.EventLog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Tests.Lambda.Sqs;

[TestFixture]
public class SqsEventHandlerTests
{
    private Mock<IMessageSerializer> mockMessageSerializer;
    private Mock<IMessageHandler<TestMessage>> mockMessageHandler;
    private Mock<IServiceScopeFactory> mockServiceScopeFactory;
    private Mock<IServiceProvider> mockServiceProvider;
    private Mock<ILoggerFactory> mockLoggerFactory;
    private Mock<IServiceScope> mockServiceScope;
    private Mock<ISqsEventLogger> mockEventHandlerLogger;

    [SetUp]
    public void Initialize()
    {
        mockMessageSerializer = new Mock<IMessageSerializer>();

        mockMessageSerializer
            .Setup(p => p.Deserialize<TestMessage>(It.IsAny<string>()))
            .Returns(() => new TestMessage());

        mockMessageHandler = new Mock<IMessageHandler<TestMessage>>();
        mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>())).Returns(Task.CompletedTask);

        mockServiceScope = new Mock<IServiceScope>();

        mockServiceScopeFactory = new Mock<IServiceScopeFactory>();

        mockServiceScopeFactory.Setup(p => p.CreateScope()).Returns(mockServiceScope.Object);

        mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(p => p.GetService(typeof(IMessageHandler<TestMessage>)))
            .Returns(mockMessageHandler.Object);
        mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockServiceScopeFactory.Object);

        mockServiceProvider
            .Setup(p => p.GetService(typeof(IMessageSerializer)))
            .Returns(mockMessageSerializer.Object);

        mockServiceScope.Setup(p => p.ServiceProvider).Returns(mockServiceProvider.Object);

        mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        mockEventHandlerLogger = new Mock<ISqsEventLogger>();

        mockServiceProvider.Setup(p => p.GetService(typeof(IEnumerable<ISqsEventLogger>)))
            .Returns(new ISqsEventLogger[] { mockEventHandlerLogger.Object });
    }

    private Kralizek.Lambda.PartialBatch.SqsEventHandler<TestMessage> CreateSystemUnderTest() =>
        CreateSystemUnderTest<Kralizek.Lambda.PartialBatch.SqsEventHandler<TestMessage>>();

    private THandler CreateSystemUnderTest<THandler>() where THandler : class
    {
        var handler = new Kralizek.Lambda.PartialBatch.SqsEventHandler<TestMessage>(mockServiceProvider.Object, mockLoggerFactory.Object) as THandler;
        if (handler is null)
        {
            throw new InvalidOperationException($"system under test {nameof(THandler)} type {typeof(THandler)} not valid");
        }

        return handler;
    }

    [Test]
    public async Task HandleAsync_resolves_MessageHandler_for_each_record()
    {
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
            }
        };

        var lambdaContext = new TestLambdaContext();

        var sut = CreateSystemUnderTest();

        await sut.HandleAsync(sqsEvent, lambdaContext);

        mockServiceProvider.Verify(p => p.GetService(typeof(IMessageHandler<TestMessage>)), Times.Exactly(sqsEvent.Records.Count));
    }

    [Test]
    public async Task HandleAsync_creates_a_scope_for_each_record()
    {
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
            }
        };

        var lambdaContext = new TestLambdaContext();

        var sut = CreateSystemUnderTest();

        await sut.HandleAsync(sqsEvent, lambdaContext);

        mockServiceScopeFactory.Verify(p => p.CreateScope(), Times.Exactly(sqsEvent.Records.Count));
    }

    [Test]
    public async Task HandleAsync_executes_NotificationHandler_for_each_record()
    {
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
            }
        };

        var lambdaContext = new TestLambdaContext();

        var sut = CreateSystemUnderTest();

        await sut.HandleAsync(sqsEvent, lambdaContext);

        mockMessageHandler.Verify(p => p.HandleAsync(It.IsAny<TestMessage>(), lambdaContext), Times.Exactly(sqsEvent.Records.Count));
    }

    [Test]
    public void HandleAsync_throws_InvalidOperation_if_NotificationHandler_is_not_registered()
    {
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
            }
        };

        var lambdaContext = new TestLambdaContext();

        mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory))).Returns(mockServiceScopeFactory.Object);

        mockServiceScope.Setup(p => p.ServiceProvider).Returns(mockServiceProvider.Object);

        var sut = CreateSystemUnderTest();

        Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(sqsEvent, lambdaContext));
    }

    [Test]
    public void HandleAsync_lets_NotificationHandler_exceptions_fly_when_not_using_sqs_batch_response()
    {
        mockMessageHandler = new Mock<IMessageHandler<TestMessage>>();
        mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>()))
            .Returns(Task.FromException(new InvalidDataException()));

        mockServiceProvider.Setup(p => p.GetService(typeof(IMessageHandler<TestMessage>)))
           .Returns(mockMessageHandler.Object);

        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
            }
        };

        var lambdaContext = new TestLambdaContext();

        var sut = CreateSystemUnderTest();

        Assert.ThrowsAsync<InvalidDataException>(() => sut.HandleAsync(sqsEvent, lambdaContext));
    }

    [Theory]
    public async Task HandleAsync_provides_sqs_batch_response(bool testErrors)
    {
        mockMessageHandler = new Mock<IMessageHandler<TestMessage>>();
        mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>()))
            .Returns(testErrors ? Task.FromException(new InvalidDataException()) : Task.CompletedTask);

        mockServiceProvider.Setup(p => p.GetService(typeof(IMessageHandler<TestMessage>)))
           .Returns(mockMessageHandler.Object);

        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "msg1",
                    Body = "{}"
                },
                new SQSEvent.SQSMessage
                {
                    MessageId = "msg2",
                    Body = "{}"
                },
            }
        };

        var lambdaContext = new TestLambdaContext();

        var sut = CreateSystemUnderTest<IRequestResponseHandler<SQSEvent, SQSBatchResponse>>();

        SQSBatchResponse batchResponse = await sut.HandleAsync(sqsEvent, lambdaContext);

        mockServiceScopeFactory.Verify(p => p.CreateScope(), Times.Exactly(sqsEvent.Records.Count));
        Assert.That(batchResponse?.BatchItemFailures, Is.Not.Null);

        var expectedBatchFailures = testErrors ? new string[] { "msg1", "msg2" } : Array.Empty<string>();
        Assert.That(batchResponse.BatchItemFailures.Select(x => x.ItemIdentifier), Is.EquivalentTo(expectedBatchFailures));

        VerifyEventHandlerLogger(mockEventHandlerLogger, testErrors, sqsEvent);
    }

    internal static void VerifyEventHandlerLogger(Mock<ISqsEventLogger> mockEventHandlerLogger, bool testErrors, SQSEvent sqsEvent)
    {
        Expression<Func<EventContext, bool>> ev = x => x.Event == sqsEvent && x.Logger != null && x.LambdaContext != null;
        Expression<Func<MessageContext, bool>> msg0 = x => x.Message == sqsEvent.Records[0] && x.Index == 0 && x.ServiceProvider != null;
        Expression<Func<MessageContext, bool>> msg1 = x => x.Message == sqsEvent.Records[1] && x.Index == 1 && x.ServiceProvider != null;
        Func<Times> errorTimes = testErrors ? Times.Once : Times.Never;

        mockEventHandlerLogger.Verify(x => x.BatchReceivedAsync(It.Is(ev)), Times.Once);
        mockEventHandlerLogger.Verify(x => x.MessageReceivedAsync(It.Is(ev), It.Is(msg0)), Times.Once);
        mockEventHandlerLogger.Verify(x => x.MessageReceivedAsync(It.Is(ev), It.Is(msg1)), Times.Once);
        mockEventHandlerLogger.Verify(x => x.PartialBatchItemFailureAsync(It.Is(ev), It.Is(msg0), It.IsAny<InvalidDataException>()), errorTimes);
        mockEventHandlerLogger.Verify(x => x.PartialBatchItemFailureAsync(It.Is(ev), It.Is(msg1), It.IsAny<InvalidDataException>()), errorTimes);
        mockEventHandlerLogger.Verify(x => x.MessageCompletedAsync(It.Is(ev), It.Is(msg0)), Times.Once);
        mockEventHandlerLogger.Verify(x => x.MessageCompletedAsync(It.Is(ev), It.Is(msg1)), Times.Once);
        mockEventHandlerLogger.Verify(x => x.BatchCompletedAsync(It.Is(ev)), Times.Once);
        mockEventHandlerLogger.VerifyNoOtherCalls();
    }
}