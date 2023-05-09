using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Logging;
using static Amazon.Lambda.SQSEvents.SQSEvent;

namespace Kralizek.Lambda.PartialBatch.EventLog
{
    internal static class ServiceProvidverExtensions
    {
        public static async Task<BatchScopeServices> CreateBatchScopeAsync(this IServiceProvider serviceProvider, ILogger logger, SQSEvent input)
        {
            // instantiate a transient CompositeSqsEventLogger with transient IEnumerable<ISqsEventLogger>
            // we don't create a true DI scope here, but we do effectively scope the ISqsEventLogger instances to a batch
            var sqsEventLogger = CompositeSqsEventLogger.CreateWithFallback(serviceProvider);
            await sqsEventLogger.BatchReceivedAsync(logger, input).ConfigureAwait(false);
            var sqsMessageIndexMap = new IndexMap<SQSMessage>(input.Records);
            return new BatchScopeServices(sqsEventLogger, sqsMessageIndexMap) with { SQSEvent = input, Logger = logger };
        }

        public sealed record class BatchScopeServices(CompositeSqsEventLogger EventLogger, IndexMap<SQSMessage> IndexMap) : IAsyncDisposable, IDisposable
        {
            private bool _disposed;

            public SQSEvent SQSEvent { get; init; } = null!;

            public ILogger Logger { get; init; } = null!;

            public async ValueTask DisposeAsync()
            {
                if (!_disposed)
                {
                    await EventLogger.BatchCompletedAsync(Logger, SQSEvent).ConfigureAwait(false);
                    _disposed = true;
                }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    EventLogger.BatchCompletedAsync(Logger, SQSEvent).ConfigureAwait(false).GetAwaiter().GetResult();
                    _disposed = true;
                }
            }
        }
    }
}
