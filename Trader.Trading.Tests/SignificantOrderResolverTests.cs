using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class SignificantOrderResolverTests
    {
        [Fact]
        public async Task ResolvesEmpty()
        {
            // arrange
            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ"
            };

            var logger = NullLogger<SignificantOrderResolver>.Instance;

            var orders = Array.Empty<OrderQueryResult>();
            var orderProvider = Mock.Of<IOrderProvider>();
            Mock.Get(orderProvider)
                .Setup(x => x.GetOrdersByFilterAsync(symbol.Name, null, false, true, CancellationToken.None))
                .ReturnsAsync(orders)
                .Verifiable();

            var trades = Array.Empty<AccountTrade>();
            var tradeProvider = Mock.Of<ITradeProvider>();
            Mock.Get(tradeProvider)
                .Setup(x => x.GetTradesAsync(symbol.Name, CancellationToken.None))
                .ReturnsAsync(trades)
                .Verifiable();

            var resolver = new SignificantOrderResolver(logger, orderProvider, tradeProvider);

            // act
            var result = await resolver.ResolveAsync(symbol, CancellationToken.None);

            // assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Empty(result.Orders);
            Assert.Empty(result.ProfitEvents);
            Assert.Empty(result.CommissionEvents);
        }
    }
}