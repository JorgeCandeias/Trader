using System.Collections.Generic;
using Trader.Models;
using Trader.Models.Comparers;

namespace Trader.Data
{
    // todo: make this class immutable
    public class SortedOrderSet : SortedSet<OrderQueryResult>
    {
        public SortedOrderSet() : base(OrderQueryResultOrderIdComparer.Instance)
        {
        }
    }
}