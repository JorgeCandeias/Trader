using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading
{
    // todo: remove when the orleans implementation is complete
    internal class TraderAgent : BackgroundService
    {
        private readonly TraderAgentOptions _options;
        private readonly ILogger _logger;
        private readonly IEnumerable<ISymbolAlgo> _algos;
        private readonly ITradingService _trader;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IGrainFactory _factory;

        public TraderAgent(IOptions<TraderAgentOptions> options, ILogger<TraderAgent> logger, IEnumerable<ISymbolAlgo> algos, ITradingService trader, IHostApplicationLifetime lifetime, IGrainFactory factory)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _algos = algos ?? throw new ArgumentNullException(nameof(algos));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private static string TypeName => nameof(TraderAgent);

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "{TypeName} querying exchange information...",
                TypeName);

            var exchangeInfo = await _trader
                .GetExchangeInfoAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var algo in _algos)
            {
                await algo
                    .InitializeAsync(exchangeInfo, cancellationToken)
                    .ConfigureAwait(false);
            }

            await base.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // wait for the application to finish starting
            var wait = new TaskCompletionSource();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStarted, stoppingToken);
            using var reg = linked.Token.Register(() => wait.SetResult());
            await wait.Task.ConfigureAwait(false);

            // keep ticking
            while (!stoppingToken.IsCancellationRequested)
            {
                // enforce a max tick time
                using var earlyTokenSource = new CancellationTokenSource(Debugger.IsAttached ? _options.TickTimeoutWithDebugger : _options.TickTimeout);
                using var safeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, earlyTokenSource.Token);

                // perform the tick
                foreach (var algo in _algos)
                {
                    try
                    {
                        await algo
                            .GoAsync(safeTokenSource.Token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "{TypeName} reports {Symbol} algorithm has faulted",
                            TypeName, algo.Symbol);
                    }
                }

                var profits = new List<(string Symbol, Profit Profit, Statistics Stats)>();
                foreach (var algo in _algos)
                {
                    var profit = await algo
                        .GetProfitAsync(safeTokenSource.Token)
                        .ConfigureAwait(false);

                    var stats = await algo
                        .GetStatisticsAsync(safeTokenSource.Token)
                        .ConfigureAwait(false);

                    profits.Add((algo.Symbol, profit, stats));
                }

                // add published profits
                var published = await _factory
                    .GetProfitAggregatorGrain()
                    .GetProfitsAsync()
                    .ConfigureAwait(false);

                foreach (var item in published)
                {
                    profits.Add((item.Symbol, item, Statistics.FromProfit(item)));
                }

                foreach (var group in profits.GroupBy(x => x.Profit.Quote).OrderBy(x => x.Key))
                {
                    _logger.LogInformation(
                        "{TypeName} reporting profit for quote {Quote}...",
                        TypeName, group.Key);

                    foreach (var item in group.OrderByDescending(x => x.Profit.Today).ThenBy(x => x.Symbol))
                    {
                        _logger.LogInformation(
                            "{TypeName} reports {Symbol,8} profit as (T: {@Today,12:F8}, T-1: {@Yesterday,12:F8}, W: {@ThisWeek,12:F8}, W-1: {@PrevWeek,12:F8}, M: {@ThisMonth,12:F8}, Y: {@ThisYear,13:F8}) (APD1: {@AveragePerDay1,12:F8}, APD7: {@AveragePerDay7,12:F8}, APD30: {@AveragePerDay30,12:F8})",
                            TypeName, item.Symbol, item.Profit.Today, item.Profit.Yesterday, item.Profit.ThisWeek, item.Profit.PrevWeek, item.Profit.ThisMonth, item.Profit.ThisYear, item.Stats.AvgPerDay1, item.Stats.AvgPerDay7, item.Stats.AvgPerDay30);
                    }

                    var totalProfit = Profit.Aggregate(group.Select(x => x.Profit));
                    var totalStats = Statistics.FromProfit(totalProfit);

                    _logger.LogInformation(
                        "{TypeName} reports {Quote,8} profit as (T: {@Today,12:F8}, T-1: {@Yesterday,12:F8}, W: {@ThisWeek,12:F8}, W-1: {@PrevWeek,12:F8}, M: {@ThisMonth,12:F8}, Y: {@ThisYear,13:F8}) (APD1: {@AveragePerDay1,12:F8}, APD7: {@AveragePerDay7,12:F8}, APD30: {@AveragePerDay30,12:F8})",
                        TypeName,
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

                await Task.Delay(_options.TickPeriod, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}