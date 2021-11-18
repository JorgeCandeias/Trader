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

    private int IndexOfKey(Symbol item)
    {
        var key = GetKeyForItem(item);

        var index = -1;
        for (var i = 0; i < Items.Count; i++)
        {
            if (GetKeyForItem(Items[i]) == key)
            {
                index = i;
                break;
            }
        }

        return index;
    }

    public void AddOrUpdate(Symbol item)
    {
        var index = IndexOfKey(item);

        if (index < 0)
        {
            Add(item);
        }
        else
        {
            this[index] = item;
        }
    }
}