using Outcompute.Trader.Models;
using System.Runtime.Serialization;

namespace Outcompute.Trader.Trading.Algorithms.Context;

[Serializable]
public class SymbolSet : SortedSet<Symbol>
{
    public SymbolSet() : base(Symbol.NameComparer)
    {
    }

    protected SymbolSet(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public void AddOrUpdate(Symbol item)
    {
        Remove(item);
        Add(item);
    }
}