using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Algorithms;
using System.Threading.Tasks;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class SignificantOrderResolverBenchmarks
    {
        private readonly ISignificantOrderResolver _resolver;

        private readonly Symbol _symbol;

        private readonly ImmutableSortedOrderSet _orders;

        private readonly ImmutableSortedTradeSet _trades;

        private readonly ILogger _logger = NullLogger.Instance;

        public SignificantOrderResolverBenchmarks()
        {
            var provider = new ServiceCollection()
                .AddSingleton<ISignificantOrderResolver, SignificantOrderResolver>()
                .AddLogging()
                .AddSqlTradingRepository(options =>
                {
                    options.ConnectionString = "server=(localdb)\\mssqllocaldb;database=trader";
                })
                .AddSystemClock()
                .AddModelAutoMapperProfiles()
                .BuildServiceProvider();

            var repository = provider.GetRequiredService<ITradingRepository>();

            _resolver = provider.GetRequiredService<ISignificantOrderResolver>();

            _symbol = Symbol.Empty with
            {
                Name = "DOGEBTC",
                BaseAsset = "DOGE",
                QuoteAsset = "BTC"
            };

            _orders = repository.GetSignificantCompletedOrdersAsync(_symbol.Name).GetAwaiter().GetResult();

            _trades = repository.GetTradesAsync(_symbol.Name).GetAwaiter().GetResult();
        }

        [Benchmark]
        public async ValueTask<SignificantResult> ResolveAsync()
        {
            return await _resolver.ResolveAsync(_symbol);
        }
    }
}