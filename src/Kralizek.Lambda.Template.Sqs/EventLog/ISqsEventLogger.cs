using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda.PartialBatch.EventLog
{
    public interface ISqsEventLogger
    {
        Task BatchReceivedAsync(ILogger logger, SQSEvent sqsEvent);
        Task MessageReceivedAsync(ILogger logger, SQSEvent sqsEvent, SQSEvent.SQSMessage sqsMessage, int index);
        Task PartialBatchItemFailureAsync(ILogger logger, SQSEvent sqsEvent, SQSEvent.SQSMessage sqsMessage, Exception exc, int index);
        Task MessageCompletedAsync(ILogger logger, SQSEvent sqsEvent, SQSEvent.SQSMessage sqsMessage, int index);
        Task BatchCompletedAsync(ILogger logger, SQSEvent sqsEvent);
    }
}