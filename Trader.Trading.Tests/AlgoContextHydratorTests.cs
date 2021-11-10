using Moq;
using Outcompute.Trader.Core;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoContextHydratorTests
    {
        [Fact]
        public async Task HydratesAlgoContextSymbol()
        {
            // arrange
            var name = "ABCXYZ";
            var symbol = Symbol.Empty with
            {
                Name = name,
                BaseAsset = "ABC",
                QuoteAsset = "XYZ"
            };

            var exchange = Mock.Of<IExchangeInfoProvider>();
            Mock.Get(exchange)
                .Setup(x => x.TryGetSymbolAsync(name, CancellationToken.None))
                .Returns(Task.FromResult<Symbol?>(symbol))
                .Verifiable();

            var resolver = Mock.Of<IAutoPositionResolver>();
            var tickers = Mock.Of<ITickerProvider>();
            var balances = Mock.Of<IBalanceProvider>();
            var savings = Mock.Of<ISavingsProvider>();
            var orders = Mock.Of<IOrderProvider>();
            var swaps = Mock.Of<ISwapPoolProvider>();
            var configurators = Array.Empty<IAlgoContextConfigurator<AlgoContext>>();
            var hydrator = new AlgoContextHydrator(exchange, resolver, tickers, balances, savings, orders, swaps, configurators);
            var context = new AlgoContext(NullServiceProvider.Instance);

            // act
            await hydrator.HydrateSymbolAsync(context, name, name, CancellationToken.None);

            // assert
            Assert.Same(symbol, context.Symbol);
            Mock.Get(exchange).VerifyAll();
        }

        [Fact]
        public async Task HydratesAlgoContextAll()
        {
            // arrange
            var name = "Algo1";

            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ"
            };
            DateTime startTime = DateTime.Today.AddDays(-10);

            var exchange = Mock.Of<IExchangeInfoProvider>();
            Mock.Get(exchange)
                .Setup(x => x.TryGetSymbolAsync(symbol.Name, CancellationToken.None))
                .Returns(Task.FromResult<Symbol?>(symbol));

            var significant = PositionDetails.Empty with
            {
                Symbol = symbol
            };
            var resolver = Mock.Of<IAutoPositionResolver>();
            Mock.Get(resolver)
                .Setup(x => x.ResolveAsync(symbol, startTime, CancellationToken.None))
                .Returns(Task.FromResult(significant));

            var ticker = MiniTicker.Empty with
            {
                Symbol = symbol.Name
            };
            var tickers = Mock.Of<ITickerProvider>();
            Mock.Get(tickers)
                .Setup(x => x.TryGetTickerAsync(symbol.Name, CancellationToken.None))
                .Returns(Task.FromResult<MiniTicker?>(ticker));

            var assetBalance = Balance.Empty with { Asset = symbol.BaseAsset };
            var quoteBalance = Balance.Empty with { Asset = symbol.QuoteAsset };
            var balances = Mock.Of<IBalanceProvider>();
            Mock.Get(balances)
                .Setup(x => x.TryGetBalanceAsync(symbol.BaseAsset, CancellationToken.None))
                .Returns(Task.FromResult<Balance?>(assetBalance));
            Mock.Get(balances)
                .Setup(x => x.TryGetBalanceAsync(symbol.QuoteAsset, CancellationToken.None))
                .Returns(Task.FromResult<Balance?>(quoteBalance));

            var assetSavings = SavingsPosition.Empty with { Asset = symbol.BaseAsset };
            var quoteSavings = SavingsPosition.Empty with { Asset = symbol.QuoteAsset };
            var savings = Mock.Of<ISavingsProvider>();
            Mock.Get(savings)
                .Setup(x => x.TryGetPositionAsync(symbol.BaseAsset, CancellationToken.None))
                .Returns(Task.FromResult<SavingsPosition?>(assetSavings));
            Mock.Get(savings)
                .Setup(x => x.TryGetPositionAsync(symbol.QuoteAsset, CancellationToken.None))
                .Returns(Task.FromResult<SavingsPosition?>(quoteSavings));

            var order = OrderQueryResult.Empty with { Symbol = symbol.Name };
            var orders = Mock.Of<IOrderProvider>();
            Mock.Get(orders)
                .Setup(x => x.GetOrdersAsync(symbol.Name, CancellationToken.None))
                .Returns(Task.FromResult<IReadOnlyList<OrderQueryResult>>(new[] { order }));

            var swaps = Mock.Of<ISwapPoolProvider>();
            var configurators = Array.Empty<IAlgoContextConfigurator<AlgoContext>>();

            var hydrator = new AlgoContextHydrator(exchange, resolver, tickers, balances, savings, orders, swaps, configurators);
            var context = new AlgoContext(NullServiceProvider.Instance);

            // act
            await hydrator.HydrateAllAsync(context, name, symbol.Name, startTime, CancellationToken.None);

            // assert
            Assert.Same(symbol, context.Symbol);
            Assert.Same(significant, context.PositionDetails);
            Assert.Same(ticker, context.Ticker);
            Assert.Same(assetBalance, context.AssetSpotBalance);
            Assert.Same(quoteBalance, context.QuoteSpotBalance);
            Assert.Same(assetSavings, context.AssetSavingsBalance);
            Assert.Same(quoteSavings, context.QuoteSavingsBalance);
            Assert.Same(order, context.Orders.Single());
        }
    }
}