using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Tests;

public class AlgoContextOrdersConfiguratorTests
{
    [Fact]
    public async Task Configures()
    {
        // arrange
        var name = "Algo1";

        var symbol = Symbol.Empty with
        {
            Name = "ABCXYZ"
        };

        var openOrders = ImmutableSortedOrderSet.Empty;
        var filledOrders = ImmutableSortedOrderSet.Empty;
        var completedOrders = ImmutableSortedOrderSet.Empty;

        var orderProvider = Mock.Of<IOrderProvider>();
        Mock.Get(orderProvider)
            .Setup(x => x.GetOrdersByFilterAsync(symbol.Name, null, true, null, CancellationToken.None))
            .ReturnsAsync(openOrders)
            .Verifiable();
        Mock.Get(orderProvider)
            .Setup(x => x.GetOrdersByFilterAsync(symbol.Name, null, false, true, CancellationToken.None))
            .ReturnsAsync(filledOrders)
            .Verifiable();
        Mock.Get(orderProvider)
            .Setup(x => x.GetOrdersAsync(symbol.Name, CancellationToken.None))
            .ReturnsAsync(completedOrders)
            .Verifiable();

        var configurator = new AlgoContextOrdersConfigurator(orderProvider);

        var provider = new ServiceCollection()
            .BuildServiceProvider();

        var context = new AlgoContext(name, provider);
        context.Symbols.AddOrUpdate(symbol);

        // act
        await configurator.ConfigureAsync(context, name);

        // assert
        Assert.Same(openOrders, context.Data[symbol.Name].Orders.Open);
        Assert.Same(filledOrders, context.Data[symbol.Name].Orders.Filled);
        Assert.Same(completedOrders, context.Data[symbol.Name].Orders.Completed);
        Mock.Get(orderProvider).VerifyAll();
    }
}