using System.Collections;

namespace Outcompute.Trader.Trading.Algorithms.Context;

/// <summary>
/// Organizes multiple shards of data for each symbol.
/// </summary>
public class SymbolDataCollection : IReadOnlyCollection<SymbolData>
{
    private readonly SortedDictionary<string, SymbolData> _data = new();

    public SymbolData this[string symbol] => _data[symbol];

    public SymbolData GetOrAdd(string symbol)
    {
        if (_data.TryGetValue(symbol, out var item))
        {
            return item;
        }

        item = new SymbolData(symbol);

        _data.Add(symbol, item);

        return item;
    }

    public int Count => _data.Count;

    public IEnumerator<SymbolData> GetEnumerator() => _data.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}