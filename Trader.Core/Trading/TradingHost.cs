using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Core.Timers;
using Trader.Core.Trading.Algorithms;
using Trader.Core.Trading.Algorithms.Accumulator;
using Trader.Core.Trading.Algorithms.Step;

namespace Trader.Core.Trading
{
    internal class TradingHost : ITradingHost, IHostedService
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<ITradingAlgorithm> _algos;
        private readonly ISafeTimer _timer;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;

        public TradingHost(ILogger<TradingHost> logger, IEnumerable<ITradingAlgorithm> algos, ISafeTimerFactory timerFactory, ITradingService trader, ISystemClock clock)
        {
            _logger = logger;
            _algos = algos;
            _trader = trader;
            _clock = clock;

            _timer = timerFactory.Create(_ => TickAsync(), TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        private readonly List<Task> _tasks = new();

        private static string Name => nameof(TradingHost);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _timer.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _timer.StopAsync(cancellationToken);
        }

        private async Task TickAsync()
        {
            // grab the exchange information once to share between all algo instances
            var exchange = await _trader.GetExchangeInfoAsync();
            var accountInfo = await _trader.GetAccountInfoAsync(new GetAccountInfo(null, _clock.UtcNow));

            _tasks.Clear();

            foreach (var algo in _algos)
            {
                _tasks.Add(algo switch
                {
                    IStepAlgorithm step => step.GoAsync(exchange, accountInfo),
                    IAccumulatorAlgorithm accumulator => accumulator.GoAsync(),
                    _ => throw new NotSupportedException($"Unknown Algorithm '{algo.GetType().FullName}'"),
                });
            }

            await Task.WhenAll(_tasks);
        }
    }
}