using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Core.Timers;
using Trader.Trading.Algorithms;

namespace Trader.Trading
{
    internal class TradingHost : IHostedService
    {
        private readonly TradingHostOptions _options;
        private readonly ILogger _logger;
        private readonly IEnumerable<ITradingAlgorithm> _algos;
        private readonly ISafeTimerFactory _timers;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;

        public TradingHost(IOptions<TradingHostOptions> options, ILogger<TradingHost> logger, IEnumerable<ITradingAlgorithm> algos, ISafeTimerFactory timers, ITradingService trader, ISystemClock clock)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _algos = algos ?? throw new ArgumentNullException(nameof(algos));
            _timers = timers ?? throw new ArgumentNullException(nameof(timers));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        private static string Name => nameof(TradingHost);

        private ISafeTimer? _timer;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var exchangeInfo = await _trader
                .GetExchangeInfoAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var algo in _algos)
            {
                await algo
                    .InitializeAsync(exchangeInfo, cancellationToken)
                    .ConfigureAwait(false);
            }

            _timer = _timers.Create(TickAsync, TimeSpan.Zero, _options.TickPeriod, Debugger.IsAttached ? _options.TickTimeoutWithDebugger : _options.TickTimeout);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Dispose();

            return Task.CompletedTask;
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Timer")]
        private async Task TickAsync(CancellationToken cancellationToken)
        {
            foreach (var algo in _algos)
            {
                try
                {
                    await algo
                        .GoAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "{Name} reports {Symbol} algorithm has faulted",
                        Name, algo.Symbol);
                }
            }

            var profits = new List<(string Symbol, Profit Profit, Statistics Stats)>();
            foreach (var algo in _algos)
            {
                var profit = await algo
                    .GetProfitAsync(cancellationToken)
                    .ConfigureAwait(false);

                var stats = await algo
                    .GetStatisticsAsync(cancellationToken)
                    .ConfigureAwait(false);

                profits.Add((algo.Symbol, profit, stats));
            }

            foreach (var group in profits.GroupBy(x => x.Profit.Quote).OrderBy(x => x.Key))
            {
                _logger.LogInformation(
                    "{Name} reporting profit for quote {Quote}...",
                    Name, group.Key);

                foreach (var item in group.OrderByDescending(x => x.Profit.Today).ThenBy(x => x.Symbol))
                {
                    _logger.LogInformation(
                        "{Name} reports {Symbol,8} profit as (T: {@Today,12:F8}, T-1: {@Yesterday,12:F8}, W: {@ThisWeek,12:F8}, W-1: {@PrevWeek,12:F8}, M: {@ThisMonth,12:F8}, Y: {@ThisYear,13:F8}) (APD1: {@AveragePerDay1,12:F8}, APD7: {@AveragePerDay7,12:F8}, APD30: {@AveragePerDay30,12:F8})",
                        Name, item.Symbol, item.Profit.Today, item.Profit.Yesterday, item.Profit.ThisWeek, item.Profit.PrevWeek, item.Profit.ThisMonth, item.Profit.ThisYear, item.Stats.AvgPerDay1, item.Stats.AvgPerDay7, item.Stats.AvgPerDay30);
                }

                var totalProfit = Profit.Aggregate(group.Select(x => x.Profit));
                var totalStats = Statistics.FromProfit(totalProfit);

                _logger.LogInformation(
                    "{Name} reports {Quote,8} profit as (T: {@Today,12:F8}, T-1: {@Yesterday,12:F8}, W: {@ThisWeek,12:F8}, W-1: {@PrevWeek,12:F8}, M: {@ThisMonth,12:F8}, Y: {@ThisYear,13:F8}) (APD1: {@AveragePerDay1,12:F8}, APD7: {@AveragePerDay7,12:F8}, APD30: {@AveragePerDay30,12:F8})",
                    Name,
                    group.Key,
                    totalProfit.Today,
                    totalProfit.Yesterday,
                    totalProfit.ThisWeek,
                    totalProfit.PrevWeek,
                    totalProfit.ThisMonth,
                    totalProfit.ThisYear,
                    totalStats.AvgPerDay1,
                    totalStats.AvgPerDay7,
                    totalStats.AvgPerDay30);
            }
        }
    }
}