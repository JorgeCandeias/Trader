using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Core.Timers;
using Trader.Data;
using Trader.Trading.Algorithms;
using Trader.Trading.Pnl;

namespace Trader.Trading
{
    internal class TradingHost : ITradingHost, IHostedService
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<ITradingAlgorithm> _algos;
        private readonly ISafeTimer _timer;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;
        private readonly IProfitCalculator _calculator;
        private readonly ITraderRepository _repository;

        public TradingHost(ILogger<TradingHost> logger, IEnumerable<ITradingAlgorithm> algos, ISafeTimerFactory timerFactory, ITradingService trader, ISystemClock clock, IProfitCalculator calculator, ITraderRepository repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _algos = algos ?? throw new ArgumentNullException(nameof(algos));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
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
                await algo.GoAsync(exchangeInfo, accountInfo, _cancellation.Token);
            }

            // calculate all pnl
            // todo: the algos themselves can do this for free while resolving significant orders
            var profits = new List<Profit>();
            foreach (var algo in _algos)
            {
                var trades = await _repository.GetTradesAsync(algo.Symbol, default, _cancellation.Token);
                var profit = _calculator.Calculate(trades);

                _logger.LogInformation(
                    "{Name} reports {Symbol} profit as {@Profit}",
                    Name, algo.Symbol, profit);

                profits.Add(profit);
            }

            _logger.LogInformation(
                "{Name} reports total profit as {@Profit}",
                Name, new Profit(
                    profits.Sum(x => x.Today),
                    profits.Sum(x => x.Yesterday),
                    profits.Sum(x => x.ThisWeek),
                    profits.Sum(x => x.ThisMonth),
                    profits.Sum(x => x.ThisYear)));
        }
    }
}