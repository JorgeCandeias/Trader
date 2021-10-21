using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    [Reentrant]
    internal class OrderProviderGrain : Grain, IOrderProviderGrain
    {
        private readonly ITradingRepository _repository;
        private readonly IHostApplicationLifetime _lifetime;

        public OrderProviderGrain(ITradingRepository repository, IHostApplicationLifetime lifetime)
        {
            _repository = repository;
            _lifetime = lifetime;
        }

        private string _symbol = Empty;

        /// <summary>
        /// Holds the order cache in a form that is mutable but still convertible to immutable upon request with low overhead.
        /// </summary>
        private readonly ImmutableSortedSet<OrderQueryResult>.Builder _orders = ImmutableSortedSet.CreateBuilder(OrderQueryResult.OrderIdComparer);

        /// <summary>
        /// An instance version that helps reset replicas upon reactivation of this grain.
        /// </summary>
        private readonly Guid _version = Guid.NewGuid();

        /// <summary>
        /// Helps tag all incoming orders so we push minimal diffs to replicas.
        /// </summary>
        private int _serial;

        /// <summary>
        /// Tag the last serial number that was saved to the repository.
        /// </summary>
        private int _savedSerial;

        /// <summary>
        /// Assigns a unique serial number to all orders.
        /// </summary>
        private readonly Dictionary<OrderQueryResult, int> _serialByOrder = new(OrderQueryResult.OrderIdEqualityComparer);

        /// <summary>
        /// Indexes orders by their latest serial number to speed up update requests.
        /// </summary>
        private readonly Dictionary<int, OrderQueryResult> _orderBySerial = new();

        /// <summary>
        /// Indexes orders by their order id to speed up requests for a single order.
        /// </summary>
        private readonly Dictionary<long, OrderQueryResult> _orderByOrderId = new();

        public override async Task OnActivateAsync()
        {
            _symbol = this.GetPrimaryKeyString();

            await LoadAsync();

            RegisterTimer(TickSaveOrdersAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            await base.OnActivateAsync();
        }

        /// <summary>
        /// Gets all cached orders.
        /// </summary>
        public Task<(Guid Version, int MaxSerial, ImmutableSortedSet<OrderQueryResult> Orders)> GetOrdersAsync()
        {
            return Task.FromResult((_version, _serial, _orders.ToImmutable()));
        }

        /// <summary>
        /// Gets the cached orders from and including the specified serial.
        /// If the specified version is different from the current version then returns all orders along with the current version.
        /// </summary>
        public Task<(Guid Version, int MaxSerial, ImmutableSortedSet<OrderQueryResult> Orders)> GetOrdersAsync(Guid version, int fromSerial)
        {
            // if the version is different then return all the orders
            if (version != _version)
            {
                return GetOrdersAsync();
            }

            // if there is nothing to return then return an empty collection
            if (fromSerial > _serial)
            {
                return Task.FromResult((_version, _serial, ImmutableSortedSet<OrderQueryResult>.Empty));
            }

            // otherwise return all new orders
            var builder = ImmutableSortedSet.CreateBuilder(OrderQueryResult.OrderIdComparer);
            for (var serial = fromSerial; serial <= _serial; serial++)
            {
                if (_orderBySerial.TryGetValue(serial, out var order))
                {
                    builder.Add(order);
                }
            }
            return Task.FromResult((_version, _serial, builder.ToImmutable()));
        }

        /// <summary>
        /// Saves the order to the cache and notifies all pending reactive pollers.
        /// </summary>
        public Task SetOrderAsync(OrderQueryResult order)
        {
            SetOrderCore(order);

            return Task.CompletedTask;
        }

        public Task<OrderQueryResult?> TryGetOrderAsync(long orderId)
        {
            var order = _orderByOrderId.TryGetValue(orderId, out var current) ? current : null;

            return Task.FromResult(order);
        }

        private void SetOrderCore(OrderQueryResult item)
        {
            // remove and unindex the old version
            if (_orders.Remove(item) && !(_orderByOrderId.Remove(item.OrderId, out _) && _serialByOrder.Remove(item, out var serial) && _orderBySerial.Remove(serial)))
            {
                throw new InvalidOperationException($"Failed to unindex order '{item.OrderId}'");
            }

            // keep the new order
            _orders.Add(item);

            // index the new order
            _orderByOrderId[item.OrderId] = item;
            _serialByOrder[item] = ++_serial;
            _orderBySerial[_serial] = item;
        }

        private Task TickSaveOrdersAsync(object _)
        {
            // break early if there is nothing to save
            if (_savedSerial == _serial) return Task.CompletedTask;

            // go on the async path only if there are orders to save
            return TickSaveOrdersCoreAsync();
        }

        /// <summary>
        /// Saves all unsaved orders from the cache to the repository.
        /// </summary>
        private async Task TickSaveOrdersCoreAsync()
        {
            // pin the current serial as it can change by interleaving tasks
            var maxSerial = _serial;

            // elect orders to save
            var elected = ArrayPool<OrderQueryResult>.Shared.Rent(maxSerial - _savedSerial + 1);
            var count = 0;

            for (var serial = _savedSerial + 1; serial <= maxSerial; ++serial)
            {
                if (_orderBySerial.TryGetValue(serial, out var order))
                {
                    elected[count++] = order;
                }
            }

            // save the items
            await _repository.SetOrdersAsync(elected.Take(count), _lifetime.ApplicationStopping);

            // mark the max serial as saved now
            _savedSerial = maxSerial;

            // cleanup
            ArrayPool<OrderQueryResult>.Shared.Return(elected);
        }

        /// <summary>
        /// Loads all orders from the repository into the cache for the current symbol.
        /// </summary>
        private async Task LoadAsync()
        {
            var orders = await _repository.GetOrdersAsync(_symbol, _lifetime.ApplicationStopping);

            foreach (var order in orders)
            {
                SetOrderCore(order);
            }
        }
    }
}