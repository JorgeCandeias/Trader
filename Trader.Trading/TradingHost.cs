using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Core.Timers;
using Trader.Models;
using Trader.Trading.Algorithms;

namespace Trader.Trading
{
    internal class TradingHost : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<ITradingAlgorithm> _algos;
        private readonly ISafeTimerFactory _timers;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;

        public TradingHost(ILogger<TradingHost> logger, IEnumerable<ITradingAlgorithm> algos, ISafeTimerFactory timers, ITradingService trader, ISystemClock clock)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _algos = algos ?? throw new ArgumentNullException(nameof(algos));
            _timers = timers ?? throw new ArgumentNullException(nameof(timers));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        private static string Name => nameof(TradingHost);

        private ISafeTimer? _timer;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = _timers.Create(TickAsync, TimeSpan.Zero, TimeSpan.FromSeconds(5), Debugger.IsAttached ? TimeSpan.FromHours(1) : TimeSpan.FromMinutes(3));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Dispose();

            return Task.CompletedTask;
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Timer")]
        private async Task TickAsync(CancellationToken cancellationToken)
        {
            // grab the exchange information once to share between all algo instances
            var exchangeInfo = await _trader
                .GetExchangeInfoAsync(cancellationToken)
                .ConfigureAwait(false);

            var accountInfo = await _trader
                .GetAccountInfoAsync(new GetAccountInfo(null, _clock.UtcNow), cancellationToken)
                .ConfigureAwait(false);

            // execute all algos in sequence for ease of troubleshooting
            foreach (var algo in _algos)
            {
                try
                {
                    await algo
                        .GoAsync(exchangeInfo, accountInfo, cancellationToken)
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

            var profits = new List<Profit>();
            foreach (var algo in _algos)
            {
                var profit = await algo
                    .GetProfitAsync(cancellationToken)
                    .ConfigureAwait(false);

                var stats = await algo
                    .GetStatisticsAsync(cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "{Name} reports {Symbol,7} profit as (T: {@Today,6:N2}, T-1: {@Yesterday,6:N2}, W: {@ThisWeek,6:N2}, W-1: {@PrevWeek,6:N2}, M: {@ThisMonth,8:N2}, Y: {@ThisYear,8:N2}) (APH1: {@AveragePerHourDay1,6:N2}, APH7: {@AveragePerHourDay7,6:N2}, APH30: {@AveragePerHourDay30,6:N2}, APD1: {@AveragePerDay1,6:N2}, APD7: {@AveragePerDay7,6:N2}, APD30: {@AveragePerDay30,6:N2})",
                    Name, algo.Symbol, profit.Today, profit.Yesterday, profit.ThisWeek, profit.PrevWeek, profit.ThisMonth, profit.ThisYear, stats.AvgPerHourDay1, stats.AvgPerHourDay7, stats.AvgPerHourDay30, stats.AvgPerDay1, stats.AvgPerDay7, stats.AvgPerDay30);

                profits.Add(profit);
            }

            var totalProfit = Profit.Aggregate(profits);
            var totalStats = Statistics.FromProfit(totalProfit);

            _logger.LogInformation(
                "{Name} reports   total profit as (T: {@Today,6:N2}, T-1: {@Yesterday,6:N2}, W: {@ThisWeek,6:N2}, W-1: {@PrevWeek,6:N2}, M: {@ThisMonth,8:N2}, Y: {@ThisYear,8:N2}) (APH1: {@AveragePerHourDay1,6:N2}, APH7: {@AveragePerHourDay7,6:N2}, APH30: {@AveragePerHourDay30,6:N2}, APD1: {@AveragePerDay1,6:N2}, APD7: {@AveragePerDay7,6:N2}, APD30: {@AveragePerDay30,6:N2})",
                Name,
                totalProfit.Today,
                totalProfit.Yesterday,
                totalProfit.ThisWeek,
                totalProfit.PrevWeek,
                totalProfit.ThisMonth,
                totalProfit.ThisYear,
                totalStats.AvgPerHourDay1,
                totalStats.AvgPerHourDay7,
                totalStats.AvgPerHourDay30,
                totalStats.AvgPerDay1,
                totalStats.AvgPerDay7,
                totalStats.AvgPerDay30);
        }
    }
}