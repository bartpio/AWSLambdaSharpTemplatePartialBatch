namespace Kralizek.Lambda.PartialBatch.EventLog
{
    internal readonly struct IndexMap<T> where T : notnull
    {
        private readonly Dictionary<T, int> _map;

        public IndexMap(IList<T> items) =>
            _map = Enumerable.Range(0, items.Count).ToDictionary(x => items[x], x => x);

        public int GetIndex(T item) =>
            _map[item];
    }
}