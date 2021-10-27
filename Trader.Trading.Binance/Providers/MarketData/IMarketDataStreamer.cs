using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal interface IMarketDataStreamer
    {
        Task StreamAsync(ISet<string> tickers, ISet<(string Symbol, KlineInterval Interval)> klines, CancellationToken cancellationToken);
    }
}