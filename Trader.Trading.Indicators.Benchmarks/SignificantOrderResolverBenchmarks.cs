using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Algorithms.Positions;
using System;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class SignificantOrderResolverBenchmarks
    {
        private readonly IAutoPositionResolver _resolver;

        private readonly Symbol _symbol;

        private readonly DateTime _startTime;

        private readonly OrderCollection _orders = OrderCollection.Empty;

        private readonly TradeCollection _trades = TradeCollection.Empty;

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
        public AutoPosition Resolve()
        {
            return _resolver.Resolve(_symbol, _orders, _trades, _startTime);
        }
    }
}