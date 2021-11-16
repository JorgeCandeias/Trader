using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Providers.Exchange;

[Reentrant]
[StatelessWorker(1)]
internal class ExchangeInfoReplicaGrain : Grain, IExchangeInfoReplicaGrain
{
    private readonly ExchangeInfoOptions _options;
    private readonly IGrainFactory _factory;

    public ExchangeInfoReplicaGrain(IOptions<ExchangeInfoOptions> options, IGrainFactory factory)
    {
        _options = options.Value;
        _factory = factory;
    }

    private ExchangeInfo _info = ExchangeInfo.Empty;
    private Guid _version = Guid.NewGuid();
    private IDictionary<string, Symbol> _symbols = ImmutableDictionary<string, Symbol>.Empty;

    public override async Task OnActivateAsync()
    {
        await RefreshAsync();

        RegisterTimer(TickTryRefreshAsync, null, _options.PropagationPeriod, _options.PropagationPeriod);

        await base.OnActivateAsync();
    }

    public ValueTask<ExchangeInfo> GetExchangeInfoAsync()
    {
        return ValueTask.FromResult(_info);
    }

    public ValueTask<Symbol?> TryGetSymbolAsync(string name)
    {
        var symbol = _symbols.TryGetValue(name, out var value) ? value : null;

        return ValueTask.FromResult(symbol);
    }

    private async Task RefreshAsync()
    {
        (_info, _version) = await _factory.GetExchangeInfoGrain().GetExchangeInfoAsync();

        Index();
    }

    private async Task TickTryRefreshAsync(object _)
    {
        var result = await _factory.GetExchangeInfoGrain().TryGetExchangeInfoAsync(_version);

        if (result.Value is not null)
        {
            _info = result.Value;
            _version = result.Version;

            Index();
        }
    }

    private void Index()
    {
        _symbols = _info.Symbols.ToDictionary(x => x.Name);
    }
}