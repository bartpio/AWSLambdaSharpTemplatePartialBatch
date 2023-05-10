using static Amazon.Lambda.SQSEvents.SQSEvent;

namespace Kralizek.Lambda.PartialBatch.EventLog
{
    /// <summary>
    /// Information about a particular SQS message being processed.
    /// </summary>
    /// <param name="Message">The raw SQS message. Intended for read access only.</param>
    /// <param name="Index">The 0-based index of the message within the batch.</param>
    /// <param name="ServiceProvider">A service provider scoped to this particular message.</param>
    public record class MessageContext(SQSMessage Message, int Index, IServiceProvider ServiceProvider);
}
