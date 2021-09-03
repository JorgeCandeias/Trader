using System.Collections.Generic;
using System.Linq;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IOrderCodeGenerator
    {
        string GetSellClientOrderId(long buyOrderId) => GetSellClientOrderId(Enumerable.Repeat(buyOrderId, 1));

        string GetSellClientOrderId(IEnumerable<long> buyOrderIds);
    }
}