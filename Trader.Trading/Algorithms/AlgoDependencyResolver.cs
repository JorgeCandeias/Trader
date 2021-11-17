using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms;

internal class AlgoDependencyResolver : IAlgoDependencyResolver
{
    private readonly IOptionsMonitor<AlgoDependencyOptions> _monitor;

    public AlgoDependencyResolver(IOptionsMonitor<AlgoDependencyOptions> monitor)
    {
        _monitor = monitor;
    }

    public ISet<string> Symbols => _monitor.CurrentValue.Symbols;

    public IDictionary<(string Symbol, KlineInterval Interval), int> Klines => _monitor.CurrentValue.Klines;
}