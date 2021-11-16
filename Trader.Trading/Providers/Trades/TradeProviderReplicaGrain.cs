using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Providers.Trades;

[Reentrant]
[StatelessWorker(1)]
internal class TradeProviderReplicaGrain : Grain, ITradeProviderReplicaGrain
{
    private readonly ReactiveOptions _reactive;
    private readonly IGrainFactory _factory;
    private readonly IHostApplicationLifetime _lifetime;

    public TradeProviderReplicaGrain(IOptions<ReactiveOptions> reactive, IGrainFactory factory, IHostApplicationLifetime lifetime)
    {
        _reactive = reactive.Value;
        _factory = factory;
        _lifetime = lifetime;
    }

    /// <summary>
    /// The symbol that this grain holds orders for.
    /// </summary>
    private string _symbol = null!;

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
    /// Holds the trade cache in a form that is mutable but still convertible to immutable upon request with low overhead.
    /// </summary>
    private readonly ImmutableSortedSet<AccountTrade>.Builder _trades = ImmutableSortedSet.CreateBuilder(AccountTrade.KeyComparer);

    /// <summary>
    /// Indexes trades by trade id to speed up requests for a single trade.
    /// </summary>
    private readonly Dictionary<long, AccountTrade> _tradeByTradeId = new();

    public override async Task OnActivateAsync()
    {
        _symbol = this.GetPrimaryKeyString();

        await LoadAsync();

        RegisterTimer(_ => PollAsync(), null, _reactive.ReactiveRecoveryDelay, _reactive.ReactiveRecoveryDelay);

        await base.OnActivateAsync();
    }

    public Task<AccountTrade?> TryGetTradeAsync(long tradeId)
    {
        var trade = _tradeByTradeId.TryGetValue(tradeId, out var current) ? current : null;

        return Task.FromResult(trade);
    }

    public Task<IReadOnlyList<AccountTrade>> GetTradesAsync()
    {
        return Task.FromResult<IReadOnlyList<AccountTrade>>(_trades.ToImmutable());
    }

    public Task SetTradeAsync(AccountTrade trade)
    {
        if (trade is null) throw new ArgumentNullException(nameof(trade));

        return SetTradeCoreAsync(trade);
    }

    private async Task SetTradeCoreAsync(AccountTrade trade)
    {
        await _factory.GetTradeProviderGrain(_symbol).SetTradeAsync(trade);

        Apply(trade);
    }

    public Task SetTradesAsync(IEnumerable<AccountTrade> trades)
    {
        if (trades is null) throw new ArgumentNullException(nameof(trades));

        return SetTradesCoreAsync(trades);
    }

    private async Task SetTradesCoreAsync(IEnumerable<AccountTrade> trades)
    {
        await _factory.GetTradeProviderGrain(_symbol).SetTradesAsync(trades);

        foreach (var trade in trades)
        {
            Apply(trade);
        }
    }

    private async Task LoadAsync()
    {
        var result = await _factory.GetTradeProviderGrain(_symbol).GetTradesAsync();

        _version = result.Version;
        _serial = result.Serial;

        foreach (var trade in result.Trades)
        {
            Apply(trade);
        }
    }

    private void Apply(AccountTrade trade)
    {
        // remove old item to allow an update
        Remove(trade);

        // add new or updated item
        _trades.Add(trade);

        // index the item
        Index(trade);
    }

    private void Remove(AccountTrade trade)
    {
        if (_trades.Remove(trade) && !Unindex(trade))
        {
            throw new InvalidOperationException($"Failed to unindex order ('{trade.Symbol}','{trade.Id}')");
        }
    }

    private void Index(AccountTrade trade)
    {
        _tradeByTradeId[trade.Id] = trade;
    }

    private bool Unindex(AccountTrade trade)
    {
        return _tradeByTradeId.Remove(trade.Id);
    }

    private async Task PollAsync()
    {
        while (!_lifetime.ApplicationStopping.IsCancellationRequested)
        {
            try
            {
                var result = await _factory
                    .GetTradeProviderGrain(_symbol)
                    .TryWaitForTradesAsync(_version, _serial + 1);

                if (result.HasValue)
                {
                    _version = result.Value.Version;
                    _serial = result.Value.Serial;

                    foreach (var item in result.Value.Trades)
                    {
                        Apply(item);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // throw on target shutdown
                return;
            }
        }
    }
}