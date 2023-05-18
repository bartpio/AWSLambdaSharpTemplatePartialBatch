namespace Kralizek.Lambda.PartialBatch.ManualBatch
{
    /// <summary>
    /// Extension methods pertaining to working with manually processed batches.
    /// </summary>
    public static class ManualBatchExtensions
    {
        /// <summary>
        /// Gets the index of a particular message in a batch.
        /// </summary>
        /// <param name="batch">Batch of messages.</param>
        /// <param name="message">Message we're inquiring about.</param>
        /// <returns>Index of the particular message within the current batch (or -1 if not found).</returns>
        public static int IndexOf<TMessage>(this IReadOnlyList<TMessage> batch, TMessage message) where TMessage : class
        {
            ArgumentNullException.ThrowIfNull(batch);
            ArgumentNullException.ThrowIfNull(message);

            if (batch is Messages<TMessage> messages)
            {
                // use fast implementation
                return messages.IndexOf(message);
            }
            else if (batch is IList<TMessage> list)
            {
                return list.IndexOf(message);
            }
            else
            {
                var cnt = batch.Count;
                for (var idx = 0; idx < cnt; idx++)
                {
                    if (message.Equals(batch[idx]))
                    {
                        return idx;
                    }
                }

                return -1;
            }
        }
    }
}
