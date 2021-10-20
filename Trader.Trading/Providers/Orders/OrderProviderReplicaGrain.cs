using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Orleans.Placement;
using Orleans.Runtime;
using Orleans.Streams;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    [Reentrant]
    [PreferLocalPlacement]
    internal class OrderProviderReplicaGrain : Grain, IOrderProviderReplicaGrain
    {
        private readonly TraderStreamOptions _options;
        private readonly ILogger _logger;
        private readonly ILocalSiloDetails _details;
        private readonly ITradingRepository _repository;

        public OrderProviderReplicaGrain(IOptions<TraderStreamOptions> options, ILogger<OrderProviderReplicaGrain> logger, ILocalSiloDetails details, ITradingRepository repository)
        {
            _options = options.Value;
            _logger = logger;
            _details = details;
            _repository = repository;
        }

        /// <summary>
        /// The target silo address of this replica.
        /// </summary>
        private SiloAddress _address = null!;

        /// <summary>
        /// The symbol that this grain holds orders for.
        /// </summary>
        private string _symbol = Empty;

        /// <summary>
        /// Holds the order cache in a form that is mutable but still convertible to immutable upon request with low overhead.
        /// </summary>
        private readonly ImmutableSortedSet<OrderQueryResult>.Builder _orders = ImmutableSortedSet.CreateBuilder(OrderQueryResult.OrderIdComparer);

        /// <summary>
        /// Indexes orders by order id to speed up requests for a single order.
        /// </summary>
        private readonly Dictionary<long, OrderQueryResult> _orderByOrderId = new();

        /// <summary>
        /// The order stream for broadcasting orders to all replicas.
        /// </summary>
        private IAsyncStream<OrderQueryResult> _stream = null!;

        public override async Task OnActivateAsync()
        {
            (_address, _symbol) = this.GetPrimaryKeys();

            if (_address != _details.SiloAddress)
            {
                _logger.LogWarning(
                    "{Name} {Symbol} instance for silo '{Address}' activated in wrong silo '{SiloAddress}' and will deactivate to allow relocation",
                    nameof(OrderProviderReplicaGrain), _symbol, _address, _details.SiloAddress);

                RegisterTimer(_ => { DeactivateOnIdle(); return Task.CompletedTask; }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }

            _stream = GetStreamProvider(_options.StreamProviderName).GetOrderStream(_symbol);

            await SubscribeAsync();

            await LoadAsync();

            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            await UnsubscribeAsync();

            await base.OnDeactivateAsync();
        }

        public Task<OrderQueryResult?> TryGetOrderAsync(long orderId)
        {
            var order = _orderByOrderId.TryGetValue(orderId, out var current) ? current : null;

            return Task.FromResult(order);
        }

        public Task<ImmutableSortedSet<OrderQueryResult>> GetOrdersAsync()
        {
            return Task.FromResult(_orders.ToImmutable());
        }

        public Task SetOrderAsync(OrderQueryResult order)
        {
            if (order is null) throw new ArgumentNullException(nameof(order));

            return SetOrderCoreAsync(order);
        }

        private async Task SetOrderCoreAsync(OrderQueryResult order)
        {
            // persist the order asap
            await _repository.SetOrderAsync(order);

            // propagate the order to other replicas (and self eventually)
            await _stream.OnNextAsync(order);

            // apply the order to this replica now so it is consistent from the point of view of the algo calling this method
            Apply(order);
        }

        private async Task LoadAsync()
        {
            var orders = await _repository.GetOrdersAsync(_symbol);

            foreach (var order in orders)
            {
                Apply(order);
            }
        }

        private void Apply(OrderQueryResult order)
        {
            // remove old order to allow an update
            if (_orders.Remove(order) && !_orderByOrderId.Remove(order.OrderId))
            {
                throw new InvalidOperationException($"Failed to unindex order '{order.OrderId}'");
            }

            // add new or updated order
            _orders.Add(order);

            // index the order
            _orderByOrderId[order.OrderId] = order;
        }

        #region Streaming

        private async Task SubscribeAsync()
        {
            var subs = await _stream.GetAllSubscriptionHandles();

            if (subs.Count > 0)
            {
                await subs[0].ResumeAsync(OnNextAsync);
                foreach (var sub in subs.Skip(1))
                {
                    await sub.UnsubscribeAsync();
                }
                return;
            }

            await _stream.SubscribeAsync(OnNextAsync);
        }

        private async Task UnsubscribeAsync()
        {
            foreach (var sub in await _stream.GetAllSubscriptionHandles())
            {
                await sub.UnsubscribeAsync();
            }
        }

        public Task OnNextAsync(OrderQueryResult order, StreamSequenceToken? token = null)
        {
            Apply(order);

            return Task.CompletedTask;
        }

        #endregion Streaming
    }
}