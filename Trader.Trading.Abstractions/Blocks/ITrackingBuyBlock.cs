﻿using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Blocks
{
    public interface ITrackingBuyBlock
    {
        Task<bool> SetTrackingBuyAsync(Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, bool redeemSavings, CancellationToken cancellationToken = default);
    }
}