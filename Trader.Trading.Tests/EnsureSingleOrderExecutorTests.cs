using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Providers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class EnsureSingleOrderExecutorTests
    {
        [Fact]
        public async Task RedeemsSavings()
        {
            // arrange
            var logger = NullLogger<EnsureSingleOrderExecutor>.Instance;
            var symbol = Symbol.Empty with { Name = "ABCXYZ", BaseAsset = "ABC", QuoteAsset = "XYZ" };

            var balance = Balance.Empty with { Asset = symbol.QuoteAsset, Free = 1000000m };
            var balances = Mock.Of<IBalanceProvider>();
            Mock.Get(balances)
                .Setup(x => x.TryGetBalanceAsync(symbol.QuoteAsset, CancellationToken.None))
                .ReturnsAsync(balance)
                .Verifiable();

            var side = OrderSide.Buy;

            var existing = new[]
            {
                OrderQueryResult.Empty with { Symbol = symbol.Name, OrderId = 123, Status = OrderStatus.New, Side = side }
            };

            var orders = Mock.Of<IOrderProvider>();
            Mock.Get(orders)
                .Setup(x => x.GetOrdersByFilterAsync(symbol.Name, OrderSide.Buy, true, null, CancellationToken.None))
                .Returns(Task.FromResult<IReadOnlyList<OrderQueryResult>>(existing))
                .Verifiable();

            var executor = new EnsureSingleOrderExecutor(logger, balances, orders);

            var cancelOrderExecutor = Mock.Of<IAlgoCommandExecutor<CancelOrderCommand>>();

            var redeemed = new RedeemSavingsEvent(true, 234000);
            var redeemSavingsExecutor = Mock.Of<IAlgoCommandExecutor<RedeemSavingsCommand, RedeemSavingsEvent>>();
            Mock.Get(redeemSavingsExecutor)
                .Setup(x => x.ExecuteAsync(It.IsAny<IAlgoContext>(), It.IsAny<RedeemSavingsCommand>(), CancellationToken.None))
                .ReturnsAsync(redeemed)
                .Verifiable();

            var createOrderExecutor = Mock.Of<IAlgoCommandExecutor<CreateOrderCommand>>();

            var provider = new ServiceCollection()
                .AddSingleton(cancelOrderExecutor)
                .AddSingleton(redeemSavingsExecutor)
                .AddSingleton(createOrderExecutor)
                .BuildServiceProvider();

            var context = new AlgoContext("Algo1", provider);

            var type = OrderType.Limit;
            var timeInForce = TimeInForce.GoodTillCanceled;
            var quantity = 1000m;
            var price = 1234m;
            var redeemSavings = true;
            var redeemSwapPool = true;
            var command = new EnsureSingleOrderCommand(symbol, side, type, timeInForce, quantity, price, redeemSavings, redeemSwapPool);

            // act
            await executor.ExecuteAsync(context, command);

            // assert
            Mock.Get(orders).VerifyAll();
            Mock.Get(cancelOrderExecutor).Verify(x => x.ExecuteAsync(context, It.Is<CancelOrderCommand>(x => x.Symbol == symbol && x.OrderId == 123), CancellationToken.None));
            Mock.Get(balances).VerifyAll();
            Mock.Get(redeemSavingsExecutor).Verify(x => x.ExecuteAsync(context, It.Is<RedeemSavingsCommand>(x => x.Asset == symbol.QuoteAsset && x.Amount == 234000), CancellationToken.None));
        }
    }
}