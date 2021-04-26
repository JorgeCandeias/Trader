using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Trader.Trading.Algorithms
{
    internal class OrderCodeGenerator : IOrderCodeGenerator
    {
        public string GetSellClientOrderId(long buyOrderId)
        {
            return GetSellClientOrderId(Enumerable.Repeat(buyOrderId, 1));
        }

        public string GetSellClientOrderId(IEnumerable<long> buyOrderIds)
        {
            _ = buyOrderIds ?? throw new ArgumentNullException(nameof(buyOrderIds));

            // for now keep the earliest order id
            return buyOrderIds.Min().ToString(CultureInfo.InvariantCulture);
        }
    }
}