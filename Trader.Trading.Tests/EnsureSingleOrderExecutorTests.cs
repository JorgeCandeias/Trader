using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class EnsureSingleOrderExecutorTests
    {
        [Fact]
        public async Task Executes()
        {
            // arrange
            var options = new SavingsOptions { SavingsRedemptionDelay = TimeSpan.Zero };
            var monitor = Mock.Of<IOptionsMonitor<SavingsOptions>>(x => x.CurrentValue == options);
            var logger = NullLogger<EnsureSingleOrderExecutor>.Instance;
            var symbol = Symbol.Empty with { Name = "ABCXYZ", BaseAsset = "ABC", QuoteAsset = "XYZ" };

            var balance = Balance.Empty with { Asset = symbol.QuoteAsset, Free = 1000000m };
            var balances = Mock.Of<IBalanceProvider>();
            Mock.Get(balances)
                .Setup(x => x.TryGetBalanceAsync(symbol.QuoteAsset, CancellationToken.None))
                .Returns(Task.FromResult<Balance?>(balance))
                .Verifiable();

            var side = OrderSide.Buy;

            var existing = new[]
            {
                OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 123, Status = OrderStatus.New, Side = side }
            };

            var orders = Mock.Of<IOrderProvider>();
            Mock.Get(orders)
                .Setup(x => x.GetOrdersByFilterAsync(symbol.Name, OrderSide.Buy, true, CancellationToken.None))
                .Returns(Task.FromResult<IReadOnlyList<OrderQueryResult>>(existing))
                .Verifiable();

            var executor = new EnsureSingleOrderExecutor(monitor, logger, balances, orders);

            var cancelOrderExecutor = Mock.Of<IAlgoCommandExecutor<CancelOrderCommand>>();

            var redeemed = new RedeemSavingsEvent(true, 234000);
            var redeemSavingsExecutor = Mock.Of<IAlgoCommandExecutor<RedeemSavingsCommand, RedeemSavingsEvent>>();
            Mock.Get(redeemSavingsExecutor)
                .Setup(x => x.ExecuteAsync(It.IsAny<IAlgoContext>(), It.IsAny<RedeemSavingsCommand>(), CancellationToken.None))
                .Returns(Task.FromResult(redeemed))
                .Verifiable();

            var createOrderExecutor = Mock.Of<IAlgoCommandExecutor<CreateOrderCommand>>();

            var provider = new ServiceCollection()
                .AddSingleton(cancelOrderExecutor)
                .AddSingleton(redeemSavingsExecutor)
                .AddSingleton(createOrderExecutor)
                .BuildServiceProvider();

            var context = new AlgoContext(provider);

            var type = OrderType.Limit;
            var timeInForce = TimeInForce.GoodTillCanceled;
            var quantity = 1000m;
            var price = 1234m;
            var redeemSavings = true;
            var command = new EnsureSingleOrderCommand(symbol, side, type, timeInForce, quantity, price, redeemSavings);

            // act
            await executor.ExecuteAsync(context, command);

            // assert
            Mock.Get(orders).VerifyAll();
            Mock.Get(cancelOrderExecutor).Verify(x => x.ExecuteAsync(context, It.Is<CancelOrderCommand>(x => x.Symbol == symbol && x.OrderId == 123), CancellationToken.None));
            Mock.Get(balances).VerifyAll();
            Mock.Get(redeemSavingsExecutor).Verify(x => x.ExecuteAsync(context, It.Is<RedeemSavingsCommand>(x => x.Asset == symbol.QuoteAsset && x.Amount == 234000), CancellationToken.None));
            Mock.Get(createOrderExecutor).Verify(x => x.ExecuteAsync(context, It.Is<CreateOrderCommand>(x =>
                x.Symbol == symbol &&
                x.Side == side &&
                x.Type == type &&
                x.TimeInForce == timeInForce &&
                x.Quantity == quantity &&
                x.Price == price),
                CancellationToken.None));
        }
    }
}