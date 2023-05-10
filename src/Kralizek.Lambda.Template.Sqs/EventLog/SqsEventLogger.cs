using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda.PartialBatch.EventLog
{
    /// <summary>
    /// Provides basic logging of batch and message progress, to the event handler's (ex. SqsEventHandler) logger.
    /// </summary>
    public sealed class SqsEventLogger : ISqsEventLogger
    {
        ValueTask ISqsEventLogger.BatchReceivedAsync(EventContext eventContext)
        {
            var (sqsEvent, logger, _) = eventContext;
            logger.LogTrace("Batch received: {batchSize} messages", sqsEvent.Records.Count);
            return ValueTask.CompletedTask;
        }

        ValueTask ISqsEventLogger.MessageReceivedAsync(EventContext eventContext, MessageContext messageContext)
        {
            eventContext.Logger.LogDebug("Message received: {Message}", messageContext.Body);
            return ValueTask.CompletedTask;
        }

        ValueTask ISqsEventLogger.PartialBatchItemFailureAsync(EventContext eventContext, MessageContext messageContext, Exception exc)
        {
            eventContext.Logger.LogError(exc, "Recording batch item failure for message {MessageId}", messageContext.MessageId);
            return ValueTask.CompletedTask;
        }

        ValueTask ISqsEventLogger.MessageCompletedAsync(EventContext eventContext, MessageContext messageContext)
        {
            eventContext.Logger.LogTrace("Message completed: {Message}", messageContext.Body);
            return ValueTask.CompletedTask;
        }

        ValueTask ISqsEventLogger.BatchCompletedAsync(EventContext eventContext)
        {
            var (sqsEvent, logger, _) = eventContext;
            logger.LogTrace("Batch completed: {batchSize} messages", sqsEvent.Records.Count);
            return ValueTask.CompletedTask;
        }
    }
}
