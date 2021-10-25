using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    [Reentrant]
    [StatelessWorker(1)]
    internal class OrderProviderReplicaGrain : Grain, IOrderProviderReplicaGrain
    {
        private readonly ReactiveOptions _reactive;
        private readonly IGrainFactory _factory;
        private readonly IHostApplicationLifetime _lifetime;

        public OrderProviderReplicaGrain(IOptions<ReactiveOptions> reactive, IGrainFactory factory, IHostApplicationLifetime lifetime)
        {
            _reactive = reactive.Value;
            _factory = factory;
            _lifetime = lifetime;
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

        public Task<OrderQueryResult?> TryGetOrderAsync(long orderId)
        {
            var order = _orderByOrderId.TryGetValue(orderId, out var current) ? current : null;

            return Task.FromResult(order);
        }

        public Task<IReadOnlyList<OrderQueryResult>> GetOrdersAsync()
        {
            return Task.FromResult<IReadOnlyList<OrderQueryResult>>(_orders.ToImmutable());
        }

        public Task SetOrderAsync(OrderQueryResult item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            return SetOrderCoreAsync(item);
        }

        private async Task SetOrderCoreAsync(OrderQueryResult item)
        {
            await _factory.GetOrderProviderGrain(_symbol).SetOrderAsync(item);

            Apply(item);
        }

        public Task SetOrdersAsync(IEnumerable<OrderQueryResult> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return SetOrdersCoreAsync(items);
        }

        private async Task SetOrdersCoreAsync(IEnumerable<OrderQueryResult> items)
        {
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
}