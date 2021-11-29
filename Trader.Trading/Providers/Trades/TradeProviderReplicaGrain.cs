using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Providers.Trades;

[Reentrant]
[StatelessWorker(1)]
internal class TradeProviderReplicaGrain : Grain, ITradeProviderReplicaGrain
{
    private readonly ReactiveOptions _reactive;
    private readonly IGrainFactory _factory;
    private readonly ITradingRepository _repository;
    private readonly IHostApplicationLifetime _lifetime;

    public TradeProviderReplicaGrain(IOptions<ReactiveOptions> reactive, IGrainFactory factory, ITradingRepository repository, IHostApplicationLifetime lifetime)
    {
        _reactive = reactive.Value;
        _factory = factory;
        _repository = repository;
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

    public Task<TradeCollection> GetTradesAsync()
    {
        return Task.FromResult(new TradeCollection(_trades.ToImmutable()));
    }

    public async Task SetTradeAsync(AccountTrade trade)
    {
        Guard.IsNotNull(trade, nameof(trade));

        await _repository.SetTradeAsync(trade, _lifetime.ApplicationStopping);

        await _factory.GetTradeProviderGrain(_symbol).SetTradeAsync(trade);

        Apply(trade);
    }

    public async Task SetTradesAsync(IEnumerable<AccountTrade> trades)
    {
        Guard.IsNotNull(trades, nameof(trades));

        await _repository.SetTradesAsync(trades, _lifetime.ApplicationStopping);

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
        // validate
        Guard.IsEqualTo(trade.Symbol, _symbol, nameof(trade.Symbol));

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
            ThrowHelper.ThrowInvalidOperationException($"Failed to unindex order ('{trade.Symbol}','{trade.Id}')");
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