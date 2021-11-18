using Orleans.Concurrency;
using System.Collections.ObjectModel;

namespace Outcompute.Trader.Models.Collections
{
    [Immutable]
    public sealed class OrderCollection : ReadOnlyCollection<OrderQueryResult>
    {
        public OrderCollection(IList<OrderQueryResult> list) : base(list)
        {
        }

        public static OrderCollection Empty { get; } = new OrderCollection(Array.Empty<OrderQueryResult>());
    }
}