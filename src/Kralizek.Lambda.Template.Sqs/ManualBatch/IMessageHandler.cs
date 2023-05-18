using Amazon.Lambda.Core;

namespace Kralizek.Lambda.PartialBatch.ManualBatch
{
    /// <summary>
    /// An interface that describes a handler for batches of SQS messages whose internal type is <typeparamref name="TMessage"/>.
    /// </summary>
    public interface IMessageHandler<in TMessage> where TMessage : class
    {
        /// <summary>
        /// Handle a batch of messages.
        /// </summary>
        /// <param name="messages">Messages making up the batch.</param>
        /// <param name="context">Useful information available within the Lambda execution environment.</param>
        /// <returns>Implementation must return an enumerable of the indicies of all succesfully processed messages.</returns>
        /// <seealso cref="ManualBatchExtensions.IndexOf{TMessage}(IReadOnlyList{TMessage}, TMessage)" />
        /// <seealso cref="SuccessMap{TMessage}" />
        Task<IEnumerable<int>> HandleAsync(IReadOnlyList<TMessage> messages, ILambdaContext context);
    }
}
