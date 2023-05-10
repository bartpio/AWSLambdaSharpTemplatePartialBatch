using System.Collections;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Logging;
using static Amazon.Lambda.SQSEvents.SQSEvent;

namespace Kralizek.Lambda.PartialBatch.EventLog
{
    /// <summary>
    /// Information about an SQS Batch.
    /// </summary>
    /// <param name="Event">The raw SQS event. Intended for read access only.</param>
    /// <param name="Logger">The event handler's (SqsEventHandler or ParallelSqsEventHandler) logger.</param>
    /// <param name="LambdaContext">The lambda context for this invocation.</param>
    public record class EventContext(SQSEvent Event, ILogger Logger, ILambdaContext LambdaContext) : IReadOnlyList<SQSMessage>
    {
        public SQSMessage this[int index] => Event.Records[index];

        public int Count => Event.Records.Count;

        public IEnumerator<SQSMessage> GetEnumerator() =>
            Event.Records.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            ((IEnumerable)Event.Records).GetEnumerator();
    }
}
