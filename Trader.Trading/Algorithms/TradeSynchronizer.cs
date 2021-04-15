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
            var tradeId = await _repository.GetMaxTradeIdAsync(symbol, cancellationToken) + 1;

            // pull all new trades
            var count = 0;
            SortedTradeSet trades;
            do
            {
                trades = await _trader.GetAccountTradesAsync(new GetAccountTrades(symbol, null, null, tradeId, 1000, null, _clock.UtcNow), cancellationToken);

                if (trades.Count > 0)
                {
                    // persist all new trades
                    await _repository.SetTradesAsync(trades, cancellationToken);

                    // set the start of the next page
                    tradeId = trades.Max!.Id + 1;

                    // keep track for logging
                    count += trades.Count;
                }
            } while (trades.Count >= 1000);

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