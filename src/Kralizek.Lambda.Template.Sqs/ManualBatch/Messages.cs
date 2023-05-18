using System.Collections;
using System.Collections.ObjectModel;

namespace Kralizek.Lambda.PartialBatch.ManualBatch
{
    /// <summary>
    /// Provides messages making up a batch, and a convenient way to look up a particular message's index within the batch.
    /// </summary>
    internal sealed class Messages<TMessage> : IReadOnlyList<TMessage> where TMessage : class 
    {
        private readonly IReadOnlyList<TMessage> _messages;
        private ReadOnlyDictionary<TMessage, int>? _indexMap;

        public Messages(IReadOnlyList<TMessage> messages)
        {
            _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        }

        /// <summary>
        /// Maps each message to its index within a batch.
        /// </summary>
        public IReadOnlyDictionary<TMessage, int> IndexMap
        {
            get
            {
                var indexMap = _indexMap;
                if (indexMap is not null)
                {
                    return indexMap;
                }
                else
                {
                    indexMap = _indexMap = new(Enumerable.Range(0, _messages.Count).ToDictionary(x => _messages[x], x => x));
                    return indexMap;
                }
            }
        }

        /// <summary>
        /// Gets the index of a particular message in a batch.
        /// </summary>
        /// <param name="message">Message we're inquiring about.</param>
        /// <returns>Index of the message within the current batch (or -1 if not found).</returns>
        public int IndexOf(TMessage message) =>
            IndexMap.TryGetValue(message, out var index) ? index : -1;

        /// <inheritdoc />
        public int Count => _messages.Count;

        /// <inheritdoc />
        public IEnumerator<TMessage> GetEnumerator() => _messages.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_messages).GetEnumerator();

        /// <inheritdoc />
        public TMessage this[int index] => _messages[index];
    }
}
