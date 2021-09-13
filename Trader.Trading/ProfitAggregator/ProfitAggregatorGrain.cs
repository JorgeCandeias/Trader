using Microsoft.Extensions.Logging;
using Orleans;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.ProfitAggregator
{
    // todo: implement local aggregator pattern
    internal class ProfitAggregatorGrain : Grain, IProfitAggregatorGrain
    {
        private readonly ILogger _logger;

        public ProfitAggregatorGrain(ILogger<ProfitAggregatorGrain> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private readonly Dictionary<string, Profit> _profits = new();

        public Task PublishAsync(IEnumerable<Profit> profits)
        {
            if (profits is null) throw new ArgumentNullException(nameof(profits));

            foreach (var profit in profits)
            {
                _profits[profit.Symbol] = profit;
            }

            return Task.CompletedTask;
        }

        public ValueTask<IEnumerable<Profit>> GetProfitsAsync()
        {
            var builder = ImmutableList.CreateBuilder<Profit>();

            foreach (var item in _profits)
            {
                builder.Add(item.Value);
            }

            return new ValueTask<IEnumerable<Profit>>(builder.ToImmutable());
        }
    }
}