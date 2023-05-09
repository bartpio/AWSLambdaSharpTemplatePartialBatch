using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Logging;
using static Amazon.Lambda.SQSEvents.SQSEvent;

namespace Kralizek.Lambda.PartialBatch.EventLog
{
    public sealed class SqsEventLogger : ISqsEventLogger
    {
        public Task BatchReceivedAsync(ILogger logger, SQSEvent sqsEvent)
        {
            logger.LogTrace("Batch received: {batchSize} messages", sqsEvent.Records.Count);
            return Task.CompletedTask;
        }

        public Task MessageReceivedAsync(ILogger logger, SQSEvent sqsEvent, SQSMessage sqsMessage, int index)
        {
            logger.LogDebug("Message received: {Message}", sqsMessage.Body);
            return Task.CompletedTask;
        }

        public Task PartialBatchItemFailureAsync(ILogger logger, SQSEvent sqsEvent, SQSMessage sqsMessage, Exception exc, int index)
        {
            logger.LogError(exc, "Recording batch item failure for message {MessageId}", sqsMessage.MessageId);
            return Task.CompletedTask;
        }

        public Task MessageCompletedAsync(ILogger logger, SQSEvent sqsEvent, SQSMessage sqsMessage, int index)
        {
            logger.LogTrace("Message completed: {Message}", sqsMessage.Body);
            return Task.CompletedTask;
        }

        public Task BatchCompletedAsync(ILogger logger, SQSEvent sqsEvent)
        {
            logger.LogTrace("Batch completed: {batchSize} messages", sqsEvent.Records.Count);
            return Task.CompletedTask;
        }
    }
}
