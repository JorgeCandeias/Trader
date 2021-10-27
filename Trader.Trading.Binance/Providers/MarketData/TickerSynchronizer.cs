using AutoMapper;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal class TickerSynchronizer : ITickerSynchronizer
    {
        private readonly ILogger _logger;
        private readonly ITickerProvider _tickers;
        private readonly ITradingService _trader;
        private readonly IMapper _mapper;

        public TickerSynchronizer(ILogger<TickerSynchronizer> logger, ITickerProvider tickers, ITradingService trader, IMapper mapper)
        {
            _logger = logger;
            _tickers = tickers;
            _trader = trader;
            _mapper = mapper;
        }

        private static string TypeName => nameof(TickerSynchronizer);

        public Task SyncAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
        {
            if (symbols is null) throw new ArgumentNullException(nameof(symbols));

            return SyncCoreAsync(symbols, cancellationToken);
        }

        private async Task SyncCoreAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "{Name} is syncing tickers for {Symbols}...",
                TypeName, symbols);

            var watch = Stopwatch.StartNew();

            // batches saving work in the background so we can keep pulling data without waiting
            var work = new ActionBlock<MiniTicker>(item => _tickers.SetTickerAsync(item, cancellationToken));

            // sync all symbols
            foreach (var symbol in symbols)
            {
                var subWatch = Stopwatch.StartNew();

                // get the current ticker from the exchange
                var result = await _trader
                    .WithBackoff()
                    .Get24hTickerPriceChangeStatisticsAsync(symbol, cancellationToken);

                // convert to the mini ticker that we use internally
                var ticker = _mapper.Map<MiniTicker>(result);

                // post for saving in the background
                work.Post(ticker);

                _logger.LogInformation(
                    "{Name} synced ticker for {Symbol} in {ElapsedMs}ms",
                    TypeName, symbol, subWatch.ElapsedMilliseconds);
            }

            // wait until all background saving work completes
            work.Complete();
            await work.Completion;

            _logger.LogInformation(
                "{Name} synced tickers for {Symbols} in {ElapsedMs}ms",
                TypeName, symbols, watch.ElapsedMilliseconds);
        }
    }
}