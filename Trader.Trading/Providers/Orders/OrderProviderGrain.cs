using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using OrleansDashboard;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    [Reentrant]
    internal class OrderProviderGrain : Grain, IOrderProviderGrain
    {
        private readonly ReactiveOptions _reactive;
        private readonly ITradingRepository _repository;
        private readonly IHostApplicationLifetime _lifetime;

        public OrderProviderGrain(IOptions<ReactiveOptions> reactive, ITradingRepository repository, IHostApplicationLifetime lifetime)
        {
            _reactive = reactive.Value;
            _repository = repository;
            _lifetime = lifetime;
        }

        /// <summary>
        /// The symbol that this grain is responsible for.
        /// </summary>
        private string _symbol = null!;

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

        /// <summary>
        /// Tracks all reactive caching requests.
        /// </summary>
        private readonly Dictionary<(Guid Version, int FromSerial), TaskCompletionSource<ReactiveResult?>> _completions = new();

        public override async Task OnActivateAsync()
        {
            _symbol = this.GetPrimaryKeyString();

            await LoadAsync();

            await base.OnActivateAsync();
        }

        public Task<OrderQueryResult?> TryGetOrderAsync(long orderId)
        {
            var order = _orderByOrderId.TryGetValue(orderId, out var current) ? current : null;

            return Task.FromResult(order);
        }

        /// <summary>
        /// Gets all cached orders.
        /// </summary>
        public Task<ReactiveResult> GetOrdersAsync()
        {
            return Task.FromResult(new ReactiveResult(_version, _serial, _orders.ToImmutable()));
        }

        /// <summary>
        /// Gets the cached orders from and including the specified serial.
        /// If the specified version is different from the current version then returns all orders along with the current version.
        /// </summary>
        [NoProfiling]
        public Task<ReactiveResult?> TryWaitForOrdersAsync(Guid version, int fromSerial)
        {
            // if the versions differ then return the entire data set
            if (version != _version)
            {
                return Task.FromResult<ReactiveResult?>(new ReactiveResult(_version, _serial, _orders.ToImmutable()));
            }

            // fulfill the request now if possible
            if (_serial >= fromSerial)
            {
                var builder = ImmutableSortedSet.CreateBuilder(OrderQueryResult.OrderIdComparer);

                for (var i = fromSerial; i <= _serial; i++)
                {
                    if (_orderBySerial.TryGetValue(i, out var kline))
                    {
                        builder.Add(kline);
                    }
                }

                return Task.FromResult<ReactiveResult?>(new ReactiveResult(_version, _serial, builder.ToImmutable()));
            }

            // otherwise let the request wait for more data
            return GetOrCreateCompletionTask(version, fromSerial).WithDefaultOnTimeout(null, _reactive.ReactivePollingTimeout, _lifetime.ApplicationStopping);
        }

        /// <summary>
        /// Loads all orders from the repository into the cache for the current symbol.
        /// </summary>
        private async Task LoadAsync()
        {
            var orders = await _repository.GetOrdersAsync(_symbol, _lifetime.ApplicationStopping);

            foreach (var order in orders)
            {
                Apply(order);
            }
        }

        /// <summary>
        /// Saves the order to the cache and notifies all pending reactive pollers.
        /// </summary>
        public Task SetOrderAsync(OrderQueryResult item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            Apply(item);

            return Task.CompletedTask;
        }

        public Task SetOrdersAsync(IEnumerable<OrderQueryResult> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                Apply(item);
            }

            return Task.CompletedTask;
        }

        private void Apply(OrderQueryResult item)
        {
            // remove old item to allow an update
            if (_orders.Remove(item) && !Unindex(item))
            {
                throw new InvalidOperationException($"Failed to unindex order ('{item.Symbol}','{item.OrderId}')");
            }

            _orders.Add(item);

            Index(item);
            Complete();
        }

        private void Index(OrderQueryResult item)
        {
            _orderByOrderId[item.OrderId] = item;
            _serialByOrder[item] = ++_serial;
            _orderBySerial[_serial] = item;
        }

        private bool Unindex(OrderQueryResult item)
        {
            return
                _orderByOrderId.Remove(item.OrderId) &&
                _serialByOrder.Remove(item, out var serial) &&
                _orderBySerial.Remove(serial);
        }

        private void Complete()
        {
            // break early if there is nothing to complete
            if (_completions.Count == 0) return;

            // elect promises for completion
            var elected = ArrayPool<(Guid Version, int FromSerial)>.Shared.Rent(_completions.Count);
            var count = 0;
            foreach (var key in _completions.Keys)
            {
                if (key.Version != _version || key.FromSerial <= _serial)
                {
                    elected[count++] = key;
                }
            }

            // remove and complete elected promises
            for (var i = 0; i < count; i++)
            {
                var key = elected[i];

                if (_completions.Remove(key, out var completion))
                {
                    Complete(completion, key.Version, key.FromSerial);
                }
            }

            // cleanup
            ArrayPool<(Guid, int)>.Shared.Return(elected);
        }

        private void Complete(TaskCompletionSource<ReactiveResult?> completion, Guid version, int fromSerial)
        {
            if (version != _version)
            {
                // complete on data reset
                completion.SetResult(new ReactiveResult(_version, _serial, _orders.ToImmutable()));
            }
            else
            {
                // complete on changes only
                var builder = ImmutableSortedSet.CreateBuilder(OrderQueryResult.KeyComparer);

                for (var s = fromSerial; s <= _serial; s++)
                {
                    if (_orderBySerial.TryGetValue(s, out var kline))
                    {
                        builder.Add(kline);
                    }
                }

                completion.SetResult(new ReactiveResult(_version, _serial, builder.ToImmutable()));
            }
        }

        private Task<ReactiveResult?> GetOrCreateCompletionTask(Guid version, int fromSerial)
        {
            if (!_completions.TryGetValue((version, fromSerial), out var completion))
            {
                _completions[(version, fromSerial)] = completion = new TaskCompletionSource<ReactiveResult?>();
            }

            return completion.Task;
        }
    }
}