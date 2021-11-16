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

    public ISet<string> Tickers => _monitor.CurrentValue.Tickers;

    public ISet<string> Balances => _monitor.CurrentValue.Balances;

    public ISet<string> AllSymbols => _monitor.CurrentValue.AllSymbols;

    public IDictionary<(string Symbol, KlineInterval Interval), int> Klines => _monitor.CurrentValue.Klines;
}