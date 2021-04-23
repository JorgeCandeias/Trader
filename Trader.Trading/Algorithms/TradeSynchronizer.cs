using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;

namespace Trader.Trading.Algorithms
{
    internal class TradeSynchronizer : ITradeSynchronizer
    {
        private readonly ILogger _logger;
        private readonly ITraderRepository _repository;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;

        public TradeSynchronizer(ILogger<TradeSynchronizer> logger, ITraderRepository repository, ITradingService trader, ISystemClock clock)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task SynchronizeTradesAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var watch = Stopwatch.StartNew();

            // get the last trade id available
            var tradeId = await _repository.GetMaxTradeIdAsync(symbol, cancellationToken);

            // pull all trades
            var count = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                // query for the next trades
                var trades = await _trader.GetAccountTradesAsync(new GetAccountTrades(symbol, null, null, tradeId + 1, 1000, null, _clock.UtcNow), cancellationToken);

                // break if we got all trades
                if (trades.Count is 0) break;

                // persist all new trades in this page
                await _repository.SetTradesAsync(trades, cancellationToken);

                // keep the last trade
                tradeId = trades.Max!.Id;

                // keep track for logging
                count += trades.Count;
            }

            // log the activity only if necessary
            if (count > 0)
            {
                _logger.LogInformation(
                    "{Name} {Symbol} pulled {Count} trades up to TradeId {MaxTradeId} in {ElapsedMs}ms",
                    nameof(TradeSynchronizer), symbol, count, tradeId, watch.ElapsedMilliseconds);
            }
        }
    }
}