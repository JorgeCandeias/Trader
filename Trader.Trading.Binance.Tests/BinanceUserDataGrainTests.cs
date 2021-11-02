using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Binance.Providers.UserData;
using Outcompute.Trader.Trading.Binance.Tests.Fakes;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    public class BinanceUserDataGrainTests
    {
        [Fact]
        public async Task Streams()
        {
            // arrange
            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ"
            };

            var listenKey = Guid.NewGuid().ToString();
            var time = DateTime.UtcNow;

            var options = Options.Create(new BinanceOptions
            {
                UserDataStreamStabilizationPeriod = TimeSpan.Zero
            });
            var logger = NullLogger<BinanceUserDataGrain>.Instance;

            var trader = Mock.Of<ITradingService>(x =>
                x.CreateUserDataStreamAsync(It.IsAny<CancellationToken>()) == Task.FromResult(listenKey));

            var executionReportMessage = ExecutionReportUserDataStreamMessage.Empty with
            {
                ExecutionType = ExecutionType.Trade,
                TradeId = 123,
                Symbol = symbol.Name,
                OrderId = 234
            };

            var completion = new TaskCompletionSource<UserDataStreamMessage>();
            var client = Mock.Of<IUserDataStreamClient>();
            Mock.Get(client)
                .SetupSequence(x => x.ReceiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(OutboundAccountPositionUserDataStreamMessage.Empty with
                {
                    EventTime = time,
                    LastAccountUpdateTime = time,
                    Balances = ImmutableList.Create(OutboundAccountPositionBalanceUserDataStreamMessage.Empty with
                    {
                        Asset = symbol.BaseAsset,
                        Free = 1000m,
                        Locked = 100m
                    })
                })
                .ReturnsAsync(executionReportMessage)
                .Returns(completion.Task);

            var streams = Mock.Of<IUserDataStreamClientFactory>(x =>
                x.Create(listenKey) == client);

            var orders = Mock.Of<IOrderSynchronizer>();
            Mock.Get(orders)
                .Setup(x => x.SynchronizeOrdersAsync(symbol.Name, It.IsAny<CancellationToken>()))
                .Verifiable();

            var trades = Mock.Of<ITradeSynchronizer>();
            Mock.Get(trades)
                .Setup(x => x.SynchronizeTradesAsync(symbol.Name, It.IsAny<CancellationToken>()))
                .Verifiable();

            var clock = Mock.Of<ISystemClock>();

            var trade = AccountTrade.Empty with
            {
                Symbol = executionReportMessage.Symbol,
                Id = executionReportMessage.TradeId
            };

            var order = OrderQueryResult.Empty with
            {
                Symbol = executionReportMessage.Symbol,
                OrderId = executionReportMessage.OrderId
            };

            var mapper = Mock.Of<IMapper>();
            Mock.Get(mapper)
                .Setup(x => x.Map<AccountTrade>(executionReportMessage))
                .Returns(trade)
                .Verifiable();
            Mock.Get(mapper)
                .Setup(x => x.Map<OrderQueryResult>(executionReportMessage))
                .Returns(order)
                .Verifiable();

            var lifetime = Mock.Of<IHostApplicationLifetime>();

            var orderProvider = Mock.Of<IOrderProvider>();
            Mock.Get(orderProvider)
                .Setup(x => x.SetOrderAsync(order, CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();

            Balance? balance = null;
            var balances = Mock.Of<IBalanceProvider>();
            Mock.Get(balances)
                .Setup(x => x.SetBalancesAsync(It.IsAny<IEnumerable<Balance>>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<Balance> b, CancellationToken ct) => balance = b.Single())
                .Returns(Task.CompletedTask)
                .Verifiable();

            var tradeProvider = Mock.Of<ITradeProvider>();
            Mock.Get(tradeProvider)
                .Setup(x => x.SetTradeAsync(trade, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var dependencies = Mock.Of<IOptionsMonitor<AlgoDependencyOptions>>();
            Mock.Get(dependencies)
                .Setup(x => x.CurrentValue.Symbols)
                .Returns(new[] { symbol.Name }.ToHashSet());

            var timers = new FakeTimerRegistry();
            var grain = new BinanceUserDataGrain(options, dependencies, logger, trader, streams, orders, trades, clock, mapper, lifetime, orderProvider, balances, tradeProvider, timers);

            // act - activate
            await grain.OnActivateAsync();

            // assert - timer was registered
            var timer = Assert.Single(timers.Entries);
            Assert.NotNull(timer.AsyncCallback);
            Assert.Null(timer.State);
            Assert.Equal(TimeSpan.FromSeconds(1), timer.DueTime);
            Assert.Equal(TimeSpan.FromSeconds(1), timer.Period);

            // act - tick timer
            await timer.AsyncCallback(timer.State);
            await Task.Delay(1000);

            // act - deactivate
            await grain.OnDeactivateAsync();

            // assert
            Mock.Get(orders).VerifyAll();
            Mock.Get(trades).VerifyAll();
            Mock.Get(balances).VerifyAll();
            Mock.Get(mapper).VerifyAll();
            Mock.Get(tradeProvider).VerifyAll();
            Mock.Get(orderProvider).VerifyAll();

            Assert.NotNull(balance);
            Assert.Equal(symbol.BaseAsset, balance!.Asset);
            Assert.Equal(1000m, balance.Free);
            Assert.Equal(100m, balance.Locked);

            // cleanup
            completion.SetResult(null!);
        }
    }
}