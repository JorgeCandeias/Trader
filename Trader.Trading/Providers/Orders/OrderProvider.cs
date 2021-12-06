using AutoMapper;
using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.Providers.Orders;

internal class OrderProvider : IOrderProvider
{
    private readonly IGrainFactory _factory;
    private readonly IMapper _mapper;

    public OrderProvider(IGrainFactory factory, IMapper mapper)
    {
        _factory = factory;
        _mapper = mapper;
    }

    public Task<long> GetLastSyncedOrderId(string symbol, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetOrderProviderGrain(symbol).GetLastSyncedOrderId();
    }

    public Task SetLastSyncedOrderId(string symbol, long orderId, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetOrderProviderGrain(symbol).SetLastSyncedOrderId(orderId);
    }

    public ValueTask<ImmutableSortedOrderSet> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetOrderProviderReplicaGrain(symbol).GetOrdersAsync();
    }

    public ValueTask<ImmutableSortedOrderSet> GetOrdersByFilterAsync(string symbol, OrderSide? side, bool? transient, bool? significant, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetOrderProviderReplicaGrain(symbol).GetOrdersByFilterAsync(side, transient, significant);
    }

    public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(order, nameof(order));

        return _factory.GetOrderProviderReplicaGrain(order.Symbol).SetOrderAsync(order);
    }

    public ValueTask<OrderQueryResult?> TryGetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetOrderProviderReplicaGrain(symbol).TryGetOrderAsync(orderId);
    }

    public Task SetOrdersAsync(string symbol, IEnumerable<OrderQueryResult> items, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));
        Guard.IsNotNull(items, nameof(items));

        return _factory.GetOrderProviderReplicaGrain(symbol).SetOrdersAsync(items);
    }

    public Task SetOrderAsync(OrderResult order, decimal stopPrice = 0m, decimal icebergQuantity = 0m, decimal originalQuoteOrderQuantity = 0m, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(order, nameof(order));

        var mapped = _mapper.Map<OrderQueryResult>(order, options =>
        {
            options.Items[nameof(OrderQueryResult.StopPrice)] = stopPrice;
            options.Items[nameof(OrderQueryResult.IcebergQuantity)] = icebergQuantity;
            options.Items[nameof(OrderQueryResult.OriginalQuoteOrderQuantity)] = originalQuoteOrderQuantity;
        });

        return SetOrderAsync(mapped, cancellationToken);
    }

    public async Task SetOrderAsync(CancelStandardOrderResult order, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(order, nameof(order));

        var original = await _factory
            .GetOrderProviderReplicaGrain(order.Symbol)
            .TryGetOrderAsync(order.OrderId)
            .ConfigureAwait(false);

        if (original is null)
        {
            ThrowHelper.ThrowInvalidOperationException($"Unable to cancel order '{order.OrderId}' because its original could not be found");
        }

        var mapped = _mapper.Map<OrderQueryResult>(order, options =>
        {
            options.Items[nameof(OrderQueryResult.StopPrice)] = original.StopPrice;
            options.Items[nameof(OrderQueryResult.IcebergQuantity)] = original.IcebergQuantity;
            options.Items[nameof(OrderQueryResult.Time)] = original.Time;
            options.Items[nameof(OrderQueryResult.UpdateTime)] = original.UpdateTime;
            options.Items[nameof(OrderQueryResult.IsWorking)] = original.IsWorking;
            options.Items[nameof(OrderQueryResult.OriginalQuoteOrderQuantity)] = original.OriginalQuoteOrderQuantity;
        });

        await SetOrderAsync(mapped, cancellationToken).ConfigureAwait(false);
    }
}