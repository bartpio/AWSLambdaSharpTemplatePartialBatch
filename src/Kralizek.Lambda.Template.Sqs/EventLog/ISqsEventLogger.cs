namespace Kralizek.Lambda.PartialBatch.EventLog
{
    public interface ISqsEventLogger
    {
        Task BatchReceivedAsync(EventContext eventContext);
        Task MessageReceivedAsync(EventContext eventContext, MessageContext messageContext);
        Task PartialBatchItemFailureAsync(EventContext eventContext, MessageContext messageContext, Exception exc);
        Task MessageCompletedAsync(EventContext eventContext, MessageContext messageContext);
        Task BatchCompletedAsync(EventContext eventContext);
    }
}