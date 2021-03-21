using System;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;

namespace Trader.Trading.Algorithms
{
    internal class TradeSynchronizer : ITradeSynchronizer
    {
        private readonly ITraderRepository _repository;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;

        public TradeSynchronizer(ITraderRepository repository, ITradingService trader, ISystemClock clock)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task SynchronizeTradesAsync(string symbol, CancellationToken cancellationToken = default)
        {
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
            } while (trades.Count > 0);
        }
    }
}