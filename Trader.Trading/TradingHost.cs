using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Core.Timers;
using Trader.Data;
using Trader.Trading.Algorithms;

namespace Trader.Trading
{
    internal class TradingHost : ITradingHost, IHostedService
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<ITradingAlgorithm> _algos;
        private readonly ISafeTimer _timer;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;
        private readonly ITraderRepository _repository;

        public TradingHost(ILogger<TradingHost> logger, IEnumerable<ITradingAlgorithm> algos, ISafeTimerFactory timerFactory, ITradingService trader, ISystemClock clock, ITraderRepository repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _algos = algos ?? throw new ArgumentNullException(nameof(algos));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));

            _timer = timerFactory.Create(_ => TickAsync(), TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        private readonly CancellationTokenSource _cancellation = new();
        private readonly List<Task> _tasks = new();

        private static string Name => nameof(TradingHost);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _timer.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellation.Cancel();

            try
            {
                await _timer.StopAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // noop
            }
        }

        private async Task TickAsync()
        {
            // grab the exchange information once to share between all algo instances
            var exchangeInfo = await _trader.GetExchangeInfoAsync();
            var accountInfo = await _trader.GetAccountInfoAsync(new GetAccountInfo(null, _clock.UtcNow));

            // execute all algos in sequence for ease of troubleshooting
            foreach (var algo in _algos)
            {
                try
                {
                    await algo.GoAsync(exchangeInfo, accountInfo, _cancellation.Token);
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
                var profit = await algo.GetProfitAsync(_cancellation.Token);
                var stats = await algo.GetStatisticsAsync(_cancellation.Token);

                _logger.LogInformation(
                    "{Name} reports {Symbol,7} profit as (T: {@Today,6:N2}, T-1: {@Yesterday,6:N2}, W: {@ThisWeek,6:N2}, W-1: {@PrevWeek,6:N2}, M: {@ThisMonth,6:N2}, Y: {@ThisYear,8:N2}) (APH1: {@AveragePerHourDay1,6:N2}, APH7: {@AveragePerHourDay7,6:N2}, APH30: {@AveragePerHourDay30,6:N2}, APD1: {@AveragePerDay1,6:N2}, APD7: {@AveragePerDay7,6:N2}, APD30: {@AveragePerDay30,6:N2})",
                    Name, algo.Symbol, profit.Today, profit.Yesterday, profit.ThisWeek, profit.PrevWeek, profit.ThisMonth, profit.ThisYear, stats.AvgPerHourDay1, stats.AvgPerHourDay7, stats.AvgPerHourDay30, stats.AvgPerDay1, stats.AvgPerDay7, stats.AvgPerDay30);

                profits.Add(profit);
            }

            var totalProfit = Profit.Aggregate(profits);
            var totalStats = Statistics.FromProfit(totalProfit);

            _logger.LogInformation(
                "{Name} reports   total profit as (T: {@Today,6:N2}, T-1: {@Yesterday,6:N2}, W: {@ThisWeek,6:N2}, W-1: {@PrevWeek,6:N2}, M: {@ThisMonth,6:N2}, Y: {@ThisYear,8:N2}) (APH1: {@AveragePerHourDay1,6:N2}, APH7: {@AveragePerHourDay7,6:N2}, APH30: {@AveragePerHourDay30,6:N2}, APD1: {@AveragePerDay1,6:N2}, APD7: {@AveragePerDay7,6:N2}, APD30: {@AveragePerDay30,6:N2})",
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