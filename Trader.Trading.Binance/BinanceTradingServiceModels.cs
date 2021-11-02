using System;

namespace Outcompute.Trader.Trading.Binance
{
    internal record GetSwapPoolLiquidity(
        long? PoolId,
        TimeSpan? RecvWindow,
        DateTime Timestamp);
}