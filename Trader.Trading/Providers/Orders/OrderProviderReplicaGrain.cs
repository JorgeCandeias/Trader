using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;
using Outcompute.Trader.Data;

namespace Outcompute.Trader.Trading.Providers.Orders;

[Reentrant]
[StatelessWorker(1)]
internal class OrderProviderReplicaGrain : Grain, IOrderProviderReplicaGrain
{
    private readonly ReactiveOptions _reactive;
    private readonly IGrainFactory _factory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ITradingRepository _repository;

    public OrderProviderReplicaGrain(IOptions<ReactiveOptions> reactive, IGrainFactory factory, IHostApplicationLifetime lifetime, ITradingRepository repository)
    {
        _reactive = reactive.Value;
        _factory = factory;
        _lifetime = lifetime;
        _repository = repository;
    }

    /// <summary>
    /// The symbol that this grain holds orders for.
    /// </summary>
    private string _symbol = null!;

    /// <summary>
    /// The serial version of this grain.
    /// Helps detect serial resets from the source grain.
    /// </summary>
    private Guid _version;

    /// <summary>
    /// The last known change serial.
    /// </summary>
    private int _serial;

    /// <summary>
    /// Holds the order cache in a form that is mutable but still convertible to immutable upon request with low overhead.
    /// </summary>
    private readonly ImmutableSortedSet<OrderQueryResult>.Builder _orders = ImmutableSortedSet.CreateBuilder(OrderQueryResult.KeyComparer);

    /// <summary>
    /// Indexes orders by order id to speed up requests for a single order.
    /// </summary>
    private readonly Dictionary<long, OrderQueryResult> _orderByOrderId = new();

    public override async Task OnActivateAsync()
    {
        _symbol = this.GetPrimaryKeyString();

        await LoadAsync();

        RegisterTimer(_ => PollAsync(), null, _reactive.ReactiveRecoveryDelay, _reactive.ReactiveRecoveryDelay);

        await base.OnActivateAsync();
    }

    public ValueTask<OrderQueryResult?> TryGetOrderAsync(long orderId)
    {
        var order = _orderByOrderId.TryGetValue(orderId, out var current) ? current : null;

        return ValueTask.FromResult(order);
    }

    public ValueTask<ImmutableSortedSet<OrderQueryResult>> GetOrdersAsync()
    {
        return ValueTask.FromResult(_orders.ToImmutable());
    }

    // todo: optimize this with specific filters for each used combination
    public ValueTask<ImmutableSortedSet<OrderQueryResult>> GetOrdersByFilterAsync(OrderSide? side, bool? transient, bool? significant)
    {
        var query = _orders.AsEnumerable();

        if (side.HasValue)
        {
            query = query.Where(x => x.Side == side.Value);
        }

        if (transient.HasValue)
        {
            query = query.Where(x => x.Status.IsTransientStatus() == transient.Value);
        }

        if (significant.HasValue)
        {
            query = query.Where(x => (x.ExecutedQuantity > 0) == significant.Value);
        }

        var result = query.ToImmutableSortedSet(OrderQueryResult.KeyComparer);

        return ValueTask.FromResult(result);
    }

    public async Task SetOrderAsync(OrderQueryResult item)
    {
        Guard.IsNotNull(item, nameof(item));

        await _repository.SetOrderAsync(item, _lifetime.ApplicationStopping);

        await _factory.GetOrderProviderGrain(_symbol).SetOrderAsync(item);

        Apply(item);
    }

    public async Task SetOrdersAsync(IEnumerable<OrderQueryResult> items)
    {
        Guard.IsNotNull(items, nameof(items));

        await _repository.SetOrdersAsync(items, _lifetime.ApplicationStopping);

        await _factory.GetOrderProviderGrain(_symbol).SetOrdersAsync(items);

        foreach (var item in items)
        {
            Apply(item);
        }
    }

    private async Task LoadAsync()
    {
        var result = await _factory.GetOrderProviderGrain(_symbol).GetOrdersAsync();

        _version = result.Version;
        _serial = result.Serial;

        foreach (var item in result.Items)
        {
            Apply(item);
        }
    }

    private void Apply(OrderQueryResult item)
    {
        // validate
        Guard.IsEqualTo(item.Symbol, _symbol, nameof(item.Symbol));

        // remove old item to allow an update
        Remove(item);

        // add new or updated item
        _orders.Add(item);

        // index the item
        Index(item);
    }

    private void Remove(OrderQueryResult item)
    {
        if (_orders.Remove(item) && !Unindex(item))
        {
            throw new InvalidOperationException($"Failed to unindex order ('{item.Symbol}','{item.OrderId}')");
        }
    }

    private void Index(OrderQueryResult item)
    {
        _orderByOrderId[item.OrderId] = item;
    }

    private bool Unindex(OrderQueryResult item)
    {
        return _orderByOrderId.Remove(item.OrderId);
    }

    private async Task PollAsync()
    {
        while (!_lifetime.ApplicationStopping.IsCancellationRequested)
        {
            try
            {
                var result = await _factory
                    .GetOrderProviderGrain(_symbol)
                    .TryWaitForOrdersAsync(_version, _serial + 1);

                if (result.HasValue)
                {
                    _version = result.Value.Version;
                    _serial = result.Value.Serial;

                    foreach (var item in result.Value.Items)
                    {
                        Apply(item);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // throw on target shutdown
                return;
            }
        }
    }
}