using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda.PartialBatch.EventLog
{
    /// <summary>
    /// Provides basic logging of batch and message progress, to the event handler's (ex. SqsEventHandler) logger.
    /// </summary>
    public sealed class SqsEventLogger : ISqsEventLogger
    {
        Task ISqsEventLogger.BatchReceivedAsync(EventContext eventContext)
        {
            var (sqsEvent, logger, _) = eventContext;
            logger.LogTrace("Batch received: {batchSize} messages", sqsEvent.Records.Count);
            return Task.CompletedTask;
        }

        Task ISqsEventLogger.MessageReceivedAsync(EventContext eventContext, MessageContext messageContext)
        {
            eventContext.Logger.LogDebug("Message received: {Message}", messageContext.Body);
            return Task.CompletedTask;
        }

        Task ISqsEventLogger.PartialBatchItemFailureAsync(EventContext eventContext, MessageContext messageContext, Exception exc)
        {
            eventContext.Logger.LogError(exc, "Recording batch item failure for message {MessageId}", messageContext.MessageId);
            return Task.CompletedTask;
        }

        Task ISqsEventLogger.MessageCompletedAsync(EventContext eventContext, MessageContext messageContext)
        {
            eventContext.Logger.LogTrace("Message completed: {Message}", messageContext.Body);
            return Task.CompletedTask;
        }

        Task ISqsEventLogger.BatchCompletedAsync(EventContext eventContext)
        {
            var (sqsEvent, logger, _) = eventContext;
            logger.LogTrace("Batch completed: {batchSize} messages", sqsEvent.Records.Count);
            return Task.CompletedTask;
        }
    }
}
