using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal interface IMarketDataStreamer
    {
        Task StreamAsync(IEnumerable<string> tickers, IEnumerable<(string Symbol, KlineInterval Interval)> klines, CancellationToken cancellationToken = default);
    }
}