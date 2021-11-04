using Outcompute.Trader.Models;
using System;

namespace Outcompute.Trader.Trading.Binance
{
    internal record GetSwapPoolLiquidity(
        long? PoolId,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);

    internal record AddSwapPoolLiquidity(
        long PoolId,
        SwapPoolLiquidityType? Type,
        string Asset,
        decimal Quantity,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);

    internal record RemoveSwapPoolLiquidity(
        long PoolId,
        SwapPoolLiquidityType Type,
        string? Asset,
        decimal ShareAmount,
        long? ReceiveWindow,
        DateTime Timestamp);

    internal record GetSwapPoolConfiguration(
        long? PoolId,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}