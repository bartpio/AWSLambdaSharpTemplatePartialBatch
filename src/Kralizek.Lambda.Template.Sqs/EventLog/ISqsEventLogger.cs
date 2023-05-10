namespace Kralizek.Lambda.PartialBatch.EventLog
{
    /// <summary>
    /// Interface that hooks in to the processing of a batch and the messages within.
    /// </summary>
    public interface ISqsEventLogger
    {
        /// <summary>
        /// Called at the start of a batch.
        /// </summary>
        ValueTask BatchReceivedAsync(EventContext eventContext);

        /// <summary>
        /// Called for each message in the batch, before processing begins.
        /// </summary>
        ValueTask MessageReceivedAsync(EventContext eventContext, MessageContext messageContext);

        /// <summary>
        /// Called for each message that experienced a failure. The exception thrown from the message handler is supplied as <paramref name="exc"/>.
        /// </summary>
        ValueTask PartialBatchItemFailureAsync(EventContext eventContext, MessageContext messageContext, Exception exc);

        /// <summary>
        /// Called for each message, after the message is done processing, or the message fails to process succesfully.
        /// </summary>
        ValueTask MessageCompletedAsync(EventContext eventContext, MessageContext messageContext);

        /// <summary>
        /// Called after all messages in a batch have been processed.
        /// </summary>
        ValueTask BatchCompletedAsync(EventContext eventContext);
    }
}