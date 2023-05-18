namespace Kralizek.Lambda.PartialBatch.ManualBatch
{
    /// <summary>
    /// Optional thread-safe helper to track the success of message processing within a manually processed batch.
    /// </summary>
    public class SuccessMap<TMessage> where TMessage : class
    {
        private Messages<TMessage> _messages;
        private readonly nint[] _nints;

        /// <summary>
        /// Construct an instance, given a batch of messages.
        /// </summary>
        /// <param name="messages">Messages making up a batch.</param>
        public SuccessMap(IReadOnlyList<TMessage> messages)
        {
            ArgumentNullException.ThrowIfNull(messages);

            if (messages is Messages<TMessage> already)
            {
                _messages = already;
            }
            else
            {
                _messages = new(messages);
            }

            // instantiate thread-safe array, with an entry per message in the batch. we'll use 1 to mean success.
            _nints = new nint[_messages.Count];
        }

        /// <summary>
        /// Record the succesful processing of a particular message.
        /// </summary>
        /// <param name="index">Index of the successfully processed message.</param>
        public void RecordSuccess(int index)
        {
            _nints[index] = 1;
        }

        /// <summary>
        /// Record succesful processing for all of the supplied messages.
        /// </summary>
        /// <param name="indices">Indices of messages processed successfully (up to this point).</param>
        public void RecordSuccess(IEnumerable<int> indices)
        {
            foreach (var index in indices)
            {
                RecordSuccess(index);
            }
        }

        /// <summary>
        /// Record the succesful processing of a particular message.
        /// </summary>
        /// <param name="message">The successfully processed message.</param>
        public void RecordSuccess(TMessage message)
        {
            _nints[_messages.IndexOf(message)] = 1;
        }

        /// <summary>
        /// Record succesful processing for all of the supplied messages.
        /// </summary>
        /// <param name="messages">Messages processed successfully (up to this point).</param>
        public void RecordSuccess(IEnumerable<TMessage> messages)
        {
            foreach (var message in messages)
            {
                RecordSuccess(message);
            }
        }

        /// <summary>
        /// Get enumerable of the indices of all messages previously recorded as succesful,
        /// suitable for returning from <see cref="IMessageHandler{TMessage}"/>.
        /// </summary>
        /// <returns>Indices of all messages previously recorded as succesful.</returns>
        public IEnumerable<int> GetSuccessfulIndices()
        {
            var cnt = _nints.Length;
            var result = new List<int>(cnt);
            for (var idx = 0; idx < cnt; idx++)
            {
                if (_nints[idx] == 1)
                {
                    result.Add(idx);
                }
            }

            return result;
        }
    }
}
