using Outcompute.Trader.Models;
using System.Collections.ObjectModel;

namespace Outcompute.Trader.Trading.Algorithms.Context;

public class SymbolCollection : KeyedCollection<string, Symbol>
{
    protected override string GetKeyForItem(Symbol item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        return item.Name;
    }

    public void AddOrUpdate(Symbol item)
    {
        var index = IndexOf(item);

        if (index < 0)
        {
            Add(item);
        }
        else
        {
            SetItem(index, item);
        }
    }
}