using Moq;
using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Data.InMemory;
using System.Collections.Immutable;
using Xunit;

namespace Trader.Trading.InMemory.Tests;

public class InMemoryTradingRepositoryTests
{
    [Fact]
    public async Task GetOrdersAsync()
    {
        // arrange
        var symbol = "ABCXYZ";
        var order = OrderQueryResult.Empty with { Symbol = symbol, OrderId = 123 };
        var orders = ImmutableSortedSet.Create(OrderQueryResult.KeyComparer, order);
        var factory = Mock.Of<IGrainFactory>(x =>
            x.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty, null).GetOrdersAsync(symbol) == Task.FromResult(orders));
        var repository = new InMemoryTradingRepository(factory);

        // act
        var result = await repository.GetOrdersAsync(symbol);

        // arrange
        Assert.Collection(result, x => Assert.Same(order, x));
    }

    [Fact]
    public async Task SetOrdersAsync()
    {
        // arrange
        var symbol = "ABCXYZ";
        var order = OrderQueryResult.Empty with { Symbol = symbol, OrderId = 123 };
        var orders = ImmutableList.Create(order);
        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty, null).SetOrdersAsync(orders))
            .Verifiable();
        var repository = new InMemoryTradingRepository(factory);

        // act
        await repository.SetOrdersAsync(orders);

        // arrange
        Mock.Get(factory).VerifyAll();
    }

    [Fact]
    public async Task SetOrderAsync()
    {
        // arrange
        var symbol = "ABCXYZ";
        var order = OrderQueryResult.Empty with { Symbol = symbol, OrderId = 123 };
        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty, null).SetOrderAsync(order))
            .Verifiable();
        var repository = new InMemoryTradingRepository(factory);

        // act
        await repository.SetOrderAsync(order);

        // arrange
        Mock.Get(factory).VerifyAll();
    }

    [Fact]
    public async Task GetKlinesAsync()
    {
        // arrange
        var symbol = "ABCXYZ";
        var interval = KlineInterval.Hours1;
        var start = DateTime.Today.Subtract(TimeSpan.FromDays(10));
        var end = DateTime.Today;
        var kline = Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = DateTime.Today };
        var klines = ImmutableSortedSet.Create(KlineComparer.Key, kline);

        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty, null).GetKlinesAsync(symbol, interval))
            .ReturnsAsync(klines)
            .Verifiable();

        var repository = new InMemoryTradingRepository(factory);

        // act
        var result = await repository.GetKlinesAsync(symbol, interval, start, end);

        // arrange
        Assert.Collection(result, x => Assert.Equal(kline, x));
        Mock.Get(factory).VerifyAll();
    }

    [Fact]
    public async Task SetKlinesAsync()
    {
        // arrange
        var symbol = "ABCXYZ";
        var interval = KlineInterval.Hours1;
        var kline = Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = DateTime.Today };
        var klines = ImmutableList.Create(kline);
        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty, null).SetKlinesAsync(klines))
            .Verifiable();
        var repository = new InMemoryTradingRepository(factory);

        // act
        await repository.SetKlinesAsync(klines);

        // arrange
        Mock.Get(factory).VerifyAll();
    }

    [Fact]
    public async Task SetKlineAsync()
    {
        // arrange
        var symbol = "ABCXYZ";
        var interval = KlineInterval.Hours1;
        var kline = Kline.Empty with { Symbol = symbol, Interval = interval, OpenTime = DateTime.Today };
        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty, null).SetKlineAsync(kline))
            .Verifiable();
        var repository = new InMemoryTradingRepository(factory);

        // act
        await repository.SetKlineAsync(kline);

        // arrange
        Mock.Get(factory).VerifyAll();
    }

    [Fact]
    public async Task TryGetTickerAsync()
    {
        // arrange
        var symbol = "ABCXYZ";
        var ticker = MiniTicker.Empty with { Symbol = symbol };
        var factory = Mock.Of<IGrainFactory>(x =>
            x.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty, null).TryGetTickerAsync(symbol) == Task.FromResult(ticker));
        var repository = new InMemoryTradingRepository(factory);

        // act
        var result = await repository.TryGetTickerAsync(symbol);

        // arrange
        Assert.Same(ticker, result);
    }

    [Fact]
    public async Task SetTickerAsync()
    {
        // arrange
        var symbol = "ABCXYZ";
        var ticker = MiniTicker.Empty with { Symbol = symbol };
        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty, null).SetTickerAsync(ticker))
            .Verifiable();
        var repository = new InMemoryTradingRepository(factory);

        // act
        await repository.SetTickerAsync(ticker);

        // arrange
        Mock.Get(factory).VerifyAll();
    }

    [Fact]
    public async Task GetTradesAsync()
    {
        // arrange
        var symbol = "ABCXYZ";
        var trade = AccountTrade.Empty with { Symbol = symbol, Id = 123 };
        var trades = ImmutableSortedSet.Create(AccountTrade.KeyComparer, trade);
        var factory = Mock.Of<IGrainFactory>(x =>
            x.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty, null).GetTradesAsync(symbol) == Task.FromResult(trades));
        var repository = new InMemoryTradingRepository(factory);

        // act
        var result = await repository.GetTradesAsync(symbol);

        // arrange
        Assert.Collection(result, x => Assert.Same(trade, x));
    }

    [Fact]
    public async Task SetTradeAsync()
    {
        // arrange
        var symbol = "ABCXYZ";
        var trade = AccountTrade.Empty with { Symbol = symbol, Id = 123 };
        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty, null).SetTradeAsync(trade))
            .Verifiable();
        var repository = new InMemoryTradingRepository(factory);

        // act
        await repository.SetTradeAsync(trade);

        // arrange
        Mock.Get(factory).VerifyAll();
    }

    [Fact]
    public async Task SetTradesAsync()
    {
        // arrange
        var symbol = "ABCXYZ";
        var trade = AccountTrade.Empty with { Symbol = symbol, Id = 123 };
        var trades = ImmutableList.Create(trade);
        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty, null).SetTradesAsync(trades))
            .Verifiable();
        var repository = new InMemoryTradingRepository(factory);

        // act
        await repository.SetTradesAsync(trades);

        // arrange
        Mock.Get(factory).VerifyAll();
    }

    [Fact]
    public async Task SetBalancesAsync()
    {
        // arrange
        var asset = "ABC";
        var balance = Balance.Zero(asset);
        var balances = ImmutableList.Create(balance);
        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty, null).SetBalancesAsync(balances))
            .Verifiable();
        var repository = new InMemoryTradingRepository(factory);

        // act
        await repository.SetBalancesAsync(balances);

        // arrange
        Mock.Get(factory).VerifyAll();
    }

    [Fact]
    public async Task TryGetBalanceAsync()
    {
        // arrange
        var asset = "ABC";
        var balance = Balance.Zero(asset);

        var factory = Mock.Of<IGrainFactory>();
        Mock.Get(factory)
            .Setup(x => x.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty, null).TryGetBalanceAsync(asset))
            .ReturnsAsync(balance)
            .Verifiable();

        var repository = new InMemoryTradingRepository(factory);

        // act
        var result = await repository.TryGetBalanceAsync(asset);

        // arrange
        Assert.Same(balance, result);
        Mock.Get(factory).VerifyAll();
    }
}