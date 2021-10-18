using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Providers
{
    internal class OrderProviderGrain : Grain, IOrderProviderGrain
    {
        private readonly ITradingRepository _repository;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ReactiveOptions _options;

        public OrderProviderGrain(ITradingRepository repository, IHostApplicationLifetime lifetime, IOptions<ReactiveOptions> options)
        {
            _repository = repository;
            _lifetime = lifetime;
            _options = options.Value;
        }

        private string _symbol = Empty;

        /// <summary>
        /// Holds the order cache in a form that is mutable but still convertible to immutable upon request with low overhead.
        /// </summary>
        private readonly ImmutableSortedSet<OrderQueryResult>.Builder _orders = ImmutableSortedSet.CreateBuilder(OrderQueryResult.OrderIdComparer);

        /// <summary>
        /// Tracks active reactive caching collection requests.
        /// </summary>
        private readonly Dictionary<int, TaskCompletionSource<(Guid Version, int MaxSerial, IReadOnlyList<OrderQueryResult> Orders)>> _requests = new();

        /// <summary>
        /// An instance version that helps reset reactive caching clients upon reactivation of this grain.
        /// </summary>
        private readonly Guid _version = Guid.NewGuid();

        /// <summary>
        /// Helps tag all incoming orders so we push minimal diffs to reactive clients.
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
        /// Indexes all orders by their latest serial number to speed up fulfilling active caching requests.
        /// </summary>
        private readonly Dictionary<int, OrderQueryResult> _orderBySerial = new();

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
        public ValueTask<(Guid Version, int MaxSerial, IReadOnlyList<OrderQueryResult> Orders)> GetOrdersAsync()
        {
            return new ValueTask<(Guid, int, IReadOnlyList<OrderQueryResult>)>((_version, _serial, _orders.ToImmutable()));
        }

        public ValueTask<(Guid Version, int MaxSerial, IReadOnlyList<OrderQueryResult> Orders)> PollOrdersAsync(Guid version, int fromSerial)
        {
            // if the version is different then return all the orders plus the new version
            if (version != _version)
            {
                return GetOrdersAsync();
            }

            // attempt to fulfill the request right now
            if (fromSerial <= _serial)
            {
                var builder = ImmutableList.CreateBuilder<OrderQueryResult>();

                for (var serial = fromSerial; serial <= _serial; serial++)
                {
                    if (_orderBySerial.TryGetValue(serial, out var order))
                    {
                        builder.Add(order);
                    }
                }

                return new ValueTask<(Guid, int, IReadOnlyList<OrderQueryResult>)>((_version, _serial, builder.ToImmutable()));
            }

            // otherwise track this poll request for future completion
            if (!_requests.TryGetValue(fromSerial, out var completion))
            {
                _requests[fromSerial] = completion = new TaskCompletionSource<(Guid, int, IReadOnlyList<OrderQueryResult> Orders)>();
            }

            // let the client wait until we have data to fulfill this request or return empty on timeout
            var wait = completion.Task.WithDefaultOnTimeout((_version, _serial, ImmutableList<OrderQueryResult>.Empty), _options.ReactivePollingTimeout, _lifetime.ApplicationStopping);
            return new ValueTask<(Guid, int, IReadOnlyList<OrderQueryResult>)>(wait);
        }

        /// <summary>
        /// Publishes new orders to the reactive caching requests that are waiting for them.
        /// </summary>
        private void Publish()
        {
            // break early if there is nothing to fulfill
            if (_requests.Count is 0) return;

            // track completed requests for removal
            var completed = ArrayPool<int>.Shared.Rent(_requests.Count);
            var count = 0;

            // attempt to fulfill all requests
            foreach (var request in _requests)
            {
                // skip if the request cannot be fulfilled yet
                if (request.Key > _serial) continue;

                // fullfill the request
                var builder = ImmutableList.CreateBuilder<OrderQueryResult>();
                for (var serial = request.Key; serial <= _serial; serial++)
                {
                    if (_orderBySerial.TryGetValue(serial, out var order))
                    {
                        builder.Add(order);
                    }
                }
                request.Value.SetResult((_version, _serial, builder.ToImmutable()));

                // elect the request for removal
                completed[count++] = request.Key;
            }

            // remove completed requests
            for (var i = 0; i < count; i++)
            {
                _requests.Remove(completed[i]);
            }

            // cleanup
            ArrayPool<int>.Shared.Return(completed);
        }

        /// <summary>
        /// Saves the order to the cache and notifies all pending reactive pollers.
        /// </summary>
        public ValueTask SetOrderAsync(OrderQueryResult order)
        {
            SetOrderCore(order);

            Publish();

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Saves all orders to the cache and notifies all pending reactive pollers.
        /// </summary>
        public ValueTask SetOrdersAsync(IReadOnlyCollection<OrderQueryResult> orders)
        {
            foreach (var item in orders)
            {
                SetOrderCore(item);
            }

            Publish();

            return ValueTask.CompletedTask;
        }

        public ValueTask<OrderQueryResult?> TryGetOrderAsync(long orderId)
        {
            var order = _orders.TryGetValue(OrderQueryResult.Empty with { OrderId = orderId }, out var current) ? current : null;

            return new ValueTask<OrderQueryResult?>(order);
        }

        private void SetOrderCore(OrderQueryResult item)
        {
            // skip if the current item is newer or otherwise make room for the new item
            if (_orders.TryGetValue(item, out var current))
            {
                if (current.UpdateTime > item.UpdateTime)
                {
                    return;
                }
                else
                {
                    // remove the old version
                    _orders.Remove(current);

                    // unindex the old version
                    if (_serialByOrder.TryGetValue(current, out var serial))
                    {
                        _orderBySerial.Remove(serial);
                        _serialByOrder.Remove(current);
                    }
                }
            }

            // keep the new order
            _orders.Add(item);

            // index the new order
            _serialByOrder[item] = ++_serial;
            _orderBySerial[_serial] = item;
        }

        /// <summary>
        /// Saves all unsaved orders from the cache to the repository.
        /// </summary>
        private async Task TickSaveOrdersAsync(object _)
        {
            // break early if there is nothing to save
            if (_savedSerial == _serial) return;

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
            await _repository.SetOrdersAsync(elected.AsSegment(0, count), _lifetime.ApplicationStopping);

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

            await SetOrdersAsync(orders);
        }
    }
}