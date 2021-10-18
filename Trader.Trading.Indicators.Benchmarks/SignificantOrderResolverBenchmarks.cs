using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System.Threading.Tasks;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class SignificantOrderResolverBenchmarks
    {
        private readonly ISignificantOrderResolver _resolver;

        private readonly Symbol _symbol;

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

            _resolver = provider.GetRequiredService<ISignificantOrderResolver>();

            _symbol = Symbol.Empty with
            {
                Name = "DOGEBTC",
                BaseAsset = "DOGE",
                QuoteAsset = "BTC"
            };
        }

        [Benchmark]
        public ValueTask<SignificantResult> ResolveAsync()
        {
            return _resolver.ResolveAsync(_symbol);
        }
    }
}