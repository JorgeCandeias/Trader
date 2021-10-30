using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Standard.ValueAveraging;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Commands.Many;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class ValueAveragingAlgoTests
    {
        [Fact]
        public async Task ReturnsClearOrdersOnNoSignals()
        {
            // arrange
            var name = "MyAlgo";

            var options = Options.Create(new ValueAveragingAlgoOptions
            {
                IsOpeningEnabled = false
            });

            var logger = NullLogger<ValueAveragingAlgo>.Instance;

            var clock = Mock.Of<ISystemClock>(
                x => x.UtcNow == DateTime.Today);

            var algo = new ValueAveragingAlgo(options, logger, clock);

            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ"
            };

            var interval = KlineInterval.Days1;

            var klines = Enumerable.Range(1, 100)
                .Select(x => Kline.Empty with { Symbol = symbol.Name, Interval = interval, OpenTime = DateTime.Today.Subtract(interval, x), ClosePrice = 1m })
                .ToImmutableSortedSet(Kline.KeyComparer);

            var klineProvider = Mock.Of<IKlineProvider>();
            Mock.Get(klineProvider)
                .Setup(x => x.GetKlinesAsync(symbol.Name, interval, CancellationToken.None))
                .Returns(Task.FromResult<IReadOnlyList<Kline>>(klines))
                .Verifiable();

            var provider = new ServiceCollection()
                .AddSingleton(klineProvider)
                .BuildServiceProvider();

            algo.Context = new AlgoContext(provider)
            {
                Name = name,
                Symbol = symbol
            };

            // act
            var result = await algo.GoAsync();

            // assert
            var command = Assert.IsType<ManyCommand>(result);
            var steps = command.Commands.ToArray();
            Assert.Equal(2, steps.Length);

            var sub0 = Assert.IsType<ClearOpenOrdersCommand>(steps[0]);
            Assert.Equal(symbol, sub0.Symbol);
            Assert.Equal(OrderSide.Buy, sub0.Side);

            var sub1 = Assert.IsType<ClearOpenOrdersCommand>(steps[1]);
            Assert.Equal(symbol, sub1.Symbol);
            Assert.Equal(OrderSide.Sell, sub1.Side);

            Mock.Get(klineProvider).VerifyAll();
        }
    }
}