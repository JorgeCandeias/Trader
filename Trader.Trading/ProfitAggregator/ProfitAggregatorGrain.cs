using Orleans;
using Outcompute.Trader.Trading.Algorithms;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.ProfitAggregator;

internal class ProfitAggregatorGrain : Grain, IProfitAggregatorGrain
{
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

    public Task<IEnumerable<Profit>> GetProfitsAsync()
    {
        var builder = ImmutableList.CreateBuilder<Profit>();

        foreach (var item in _profits)
        {
            builder.Add(item.Value);
        }

        return Task.FromResult<IEnumerable<Profit>>(builder.ToImmutable());
    }
}