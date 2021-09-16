using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Trading.Algorithms;
using Polly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading
{
    // todo: remove this class once there is a basic interface in place
    internal class TraderAgent : BackgroundService
    {
        private readonly TraderAgentOptions _options;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IGrainFactory _factory;

        public TraderAgent(IOptions<TraderAgentOptions> options, ILogger<TraderAgent> logger, IHostApplicationLifetime lifetime, IGrainFactory factory)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private static string TypeName => nameof(TraderAgent);

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
                // query published profits
                var published = await Policy
                    .Handle<Exception>()
                    .RetryForeverAsync(ex =>
                    {
                        _logger.LogError(ex, "{TypeName} failed to query profit", TypeName);
                    })
                    .ExecuteAsync(_ => _factory.GetProfitAggregatorGrain().GetProfitsAsync(), stoppingToken)
                    .ConfigureAwait(false);

                // compose stats items for display
                var profits = published
                    .Select(x => (x.Symbol, Profit: x, Stats: Statistics.FromProfit(x)))
                    .GroupBy(x => x.Profit.Quote)
                    .OrderBy(x => x.Key);

                foreach (var group in profits)
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