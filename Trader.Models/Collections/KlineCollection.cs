using Orleans.Concurrency;
using System.Collections.ObjectModel;

namespace Outcompute.Trader.Models.Collections
{
    // todo: make this collection a proper immutable
    [Immutable]
    public class KlineCollection : ReadOnlyCollection<Kline>
    {
        public KlineCollection(IList<Kline> list) : base(list)
        {
        }

        public static KlineCollection Empty { get; } = new KlineCollection(Array.Empty<Kline>());
    }
}