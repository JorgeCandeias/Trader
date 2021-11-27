using Orleans.Concurrency;
using System.Collections.ObjectModel;

namespace Outcompute.Trader.Models.Collections
{
    // todo: make this collection a proper immutable
    [Immutable]
    public sealed class OrderCollection : ReadOnlyCollection<OrderQueryResult>
    {
        public OrderCollection(IList<OrderQueryResult> list) : base(list)
        {
        }

        public OrderCollection(params OrderQueryResult[] items) : base(items)
        {
        }

        public static OrderCollection Empty { get; } = new OrderCollection(Array.Empty<OrderQueryResult>());
    }
}