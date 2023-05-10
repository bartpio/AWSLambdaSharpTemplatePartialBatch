using static Amazon.Lambda.SQSEvents.SQSEvent;

namespace Kralizek.Lambda.PartialBatch.EventLog
{
    internal static class ServiceProvidverExtensions
    {
        public static async Task<BatchScopeServices> CreateBatchScopeAsync(this IServiceProvider serviceProvider, EventContext eventContext)
        {
            // instantiate a transient CompositeSqsEventLogger with transient IEnumerable<ISqsEventLogger>
            // we don't create a true DI scope here, but we do effectively scope the ISqsEventLogger instances to a batch
            ISqsEventLogger sqsEventLogger = CompositeSqsEventLogger.CreateWithFallback(serviceProvider);
            await sqsEventLogger.BatchReceivedAsync(eventContext).ConfigureAwait(false);
            var sqsMessageIndexMap = new IndexMap<SQSMessage>(eventContext.Event.Records);
            return new BatchScopeServices(sqsEventLogger, sqsMessageIndexMap) with { EventContext = eventContext };
        }

        public sealed record class BatchScopeServices(ISqsEventLogger EventLogger, IndexMap<SQSMessage> IndexMap) : IAsyncDisposable, IDisposable
        {
            private bool _disposed;

            public EventContext EventContext { get; init; } = null!;

            public async ValueTask DisposeAsync()
            {
                if (!_disposed)
                {
                    await EventLogger.BatchCompletedAsync(EventContext).ConfigureAwait(false);
                    _disposed = true;
                }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    EventLogger.BatchCompletedAsync(EventContext).ConfigureAwait(false).GetAwaiter().GetResult();
                    _disposed = true;
                }
            }
        }
    }
}
