using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Threading.Tasks;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class SignificantOrderResolverBenchmarks
    {
        private readonly IAutoPositionResolver _resolver;

        private readonly Symbol _symbol;

        private readonly DateTime _startTime;

        public SignificantOrderResolverBenchmarks()
        {
            var provider = new ServiceCollection()
                .AddSingleton<IAutoPositionResolver, AutoPositionResolver>()
                .AddLogging()
                .AddSqlTradingRepository(options =>
                {
                    options.ConnectionString = "server=(localdb)\\mssqllocaldb;database=trader";
                })
                .AddTraderCoreServices()
                .AddModelServices()
                .BuildServiceProvider();

            _resolver = provider.GetRequiredService<IAutoPositionResolver>();

            _symbol = Symbol.Empty with
            {
                Name = "DOGEBTC",
                BaseAsset = "DOGE",
                QuoteAsset = "BTC"
            };

            _startTime = DateTime.MinValue;
        }

        [Benchmark]
        public Task<PositionDetails> ResolveAsync()
        {
            return _resolver.ResolveAsync(_symbol, _startTime);
        }
    }
}