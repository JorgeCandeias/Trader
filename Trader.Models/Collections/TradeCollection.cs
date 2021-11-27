using Orleans.Concurrency;
using System.Collections.ObjectModel;

namespace Outcompute.Trader.Models.Collections
{
    // todo: make this collection a proper immutable
    [Immutable]
    public class TradeCollection : ReadOnlyCollection<AccountTrade>
    {
        public TradeCollection(IList<AccountTrade> list) : base(list)
        {
        }

        public static TradeCollection Empty { get; } = new TradeCollection(Array.Empty<AccountTrade>());
    }
}