using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.Algorithms.Context
{
    /// <summary>
    /// Organizes multiple shards of data for each symbol.
    /// </summary>
    public class SymbolDataCollection : SortedKeyedCollection<string, SymbolData>
    {
        protected override string GetKeyForItem(SymbolData item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            return item.Name;
        }

        public SymbolData GetOrAdd(string key)
        {
            if (TryGetValue(key, out var item))
            {
                return item;
            }

            item = new SymbolData(key);

            Add(item);

            return item;
        }
    }
}