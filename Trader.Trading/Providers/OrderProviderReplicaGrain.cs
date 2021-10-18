using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Providers
{
    [StatelessWorker(1)]
    internal class OrderProviderReplicaGrain : Grain, IOrderProviderReplicaGrain
    {
        private readonly ReactiveOptions _options;
        private readonly IGrainFactory _factory;

        public OrderProviderReplicaGrain(IOptions<ReactiveOptions> options, IGrainFactory factory)
        {
            _options = options.Value;
            _factory = factory;
        }

        /// <summary>
        /// The symbol that this grain holds orders for.
        /// </summary>
        private string _symbol = Empty;

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
        private readonly ImmutableSortedSet<OrderQueryResult>.Builder _orders = ImmutableSortedSet.CreateBuilder(OrderQueryResult.OrderIdComparer);

        public override async Task OnActivateAsync()
        {
            _symbol = this.GetPrimaryKeyString();

            await LoadAsync();

            RegisterTimer(TickUpdateAsync, null, _options.ReactiveTickDelay, _options.ReactiveTickDelay);

            await base.OnActivateAsync();
        }

        public ValueTask<IReadOnlyList<OrderQueryResult>> GetOrdersAsync()
        {
            return new ValueTask<IReadOnlyList<OrderQueryResult>>(_orders.ToImmutable());
        }

        private async Task LoadAsync()
        {
            // get all the orders
            var result = await _factory.GetOrderProviderGrain(_symbol).GetOrdersAsync();

            Apply(result.Version, result.MaxSerial, result.Orders);
        }

        private async Task TickUpdateAsync(object _)
        {
            // wait for new orders
            var result = await _factory.GetOrderProviderGrain(_symbol).PollOrdersAsync(_version, _serial + 1);

            Apply(result.Version, result.MaxSerial, result.Orders);
        }

        private void Apply(Guid version, int serial, IEnumerable<OrderQueryResult> orders)
        {
            // keep the new markers
            _version = version;
            _serial = serial;

            // apply new orders
            foreach (var order in orders)
            {
                // remove old order to allow an update
                _orders.Remove(order);

                // add new or updated order
                _orders.Add(order);
            }
        }
    }
}