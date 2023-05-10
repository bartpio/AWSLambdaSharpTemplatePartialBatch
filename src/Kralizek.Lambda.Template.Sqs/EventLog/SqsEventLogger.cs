using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda.PartialBatch.EventLog
{
    public sealed class SqsEventLogger : ISqsEventLogger
    {
        public Task BatchReceivedAsync(EventContext eventContext)
        {
            var (sqsEvent, logger, _) = eventContext;
            logger.LogTrace("Batch received: {batchSize} messages", sqsEvent.Records.Count);
            return Task.CompletedTask;
        }

        public Task MessageReceivedAsync(EventContext eventContext, MessageContext messageContext)
        {
            var sqsMessage = eventContext[messageContext.Index];
            eventContext.Logger.LogDebug("Message received: {Message}", sqsMessage.Body);
            return Task.CompletedTask;
        }

        public Task PartialBatchItemFailureAsync(EventContext eventContext, MessageContext messageContext, Exception exc)
        {
            var sqsMessage = eventContext[messageContext.Index];
            eventContext.Logger.LogError(exc, "Recording batch item failure for message {MessageId}", sqsMessage.MessageId);
            return Task.CompletedTask;
        }

        public Task MessageCompletedAsync(EventContext eventContext, MessageContext messageContext)
        {
            var sqsMessage = eventContext[messageContext.Index];
            eventContext.Logger.LogTrace("Message completed: {Message}", sqsMessage.Body);
            return Task.CompletedTask;
        }

        public Task BatchCompletedAsync(EventContext eventContext)
        {
            var (sqsEvent, logger, _) = eventContext;
            logger.LogTrace("Batch completed: {batchSize} messages", sqsEvent.Records.Count);
            return Task.CompletedTask;
        }
    }
}
