using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;
using Orleans.Runtime;
using OrleansDashboard;
using Outcompute.Trader.Data;
using System.Buffers;

namespace Outcompute.Trader.Trading.Providers.Trades;

[Reentrant]
internal class TradeProviderGrain : Grain, ITradeProviderGrain
{
    private readonly ReactiveOptions _reactive;
    private readonly IPersistentState<TradeProviderGrainState> _state;
    private readonly ITradingRepository _repository;
    private readonly IHostApplicationLifetime _lifetime;

    public TradeProviderGrain(IOptions<ReactiveOptions> reactive, [PersistentState("Main")] IPersistentState<TradeProviderGrainState> state, ITradingRepository repository, IHostApplicationLifetime lifetime)
    {
        _reactive = reactive.Value;
        _state = state;
        _repository = repository;
        _lifetime = lifetime;
    }

    /// <summary>
    /// The symbol that this grain is responsible for.
    /// </summary>
    private string _symbol = null!;

    /// <summary>
    /// Holds the trade cache in a form that is mutable but still convertible to immutable upon request with low overhead.
    /// </summary>
    private readonly ImmutableSortedSet<AccountTrade>.Builder _trades = ImmutableSortedSet.CreateBuilder(AccountTrade.KeyComparer);

    /// <summary>
    /// An instance version that helps reset replicas upon reactivation of this grain.
    /// </summary>
    private readonly Guid _version = Guid.NewGuid();

    /// <summary>
    /// Helps tag all incoming trades so we push minimal diffs to replicas.
    /// </summary>
    private int _serial;

    /// <summary>
    /// Assigns a unique serial number to all trades.
    /// </summary>
    private readonly Dictionary<AccountTrade, int> _serialByTrade = new(AccountTrade.TradeKeyEqualityComparer);

    /// <summary>
    /// Indexes trades by their latest serial number to speed up update requests.
    /// </summary>
    private readonly Dictionary<int, AccountTrade> _tradeBySerial = new();

    /// <summary>
    /// Indexes trades by their trade id to speed up requests for a single order.
    /// </summary>
    private readonly Dictionary<long, AccountTrade> _tradeByTradeId = new();

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

    public Task SetLastSyncedTradeIdAsync(long tradeId)
    {
        _state.State.LastSyncedTradeId = tradeId;

        return _state.WriteStateAsync();
    }

    public Task<long> GetLastSyncedTradeIdAsync()
    {
        return Task.FromResult(_state.State.LastSyncedTradeId);
    }

    public Task<AccountTrade?> TryGetTradeAsync(long tradeId)
    {
        var trade = _tradeByTradeId.TryGetValue(tradeId, out var current) ? current : null;

        return Task.FromResult(trade);
    }

    /// <summary>
    /// Gets all cached trades.
    /// </summary>
    public Task<ReactiveResult> GetTradesAsync()
    {
        return Task.FromResult(new ReactiveResult(_version, _serial, _trades.ToImmutable()));
    }

    /// <summary>
    /// Gets the cached trades from and including the specified serial.
    /// If the specified version is different from the current version then returns all trades along with the current version.
    /// </summary>
    [NoProfiling]
    public Task<ReactiveResult?> TryWaitForTradesAsync(Guid version, int fromSerial)
    {
        // if the versions differ then return the entire data set
        if (version != _version)
        {
            return Task.FromResult<ReactiveResult?>(new ReactiveResult(_version, _serial, _trades.ToImmutable()));
        }

        // fulfill the request now if possible
        if (_serial >= fromSerial)
        {
            var builder = ImmutableSortedSet.CreateBuilder(AccountTrade.KeyComparer);

            for (var i = fromSerial; i <= _serial; i++)
            {
                if (_tradeBySerial.TryGetValue(i, out var kline))
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
    /// Loads all trades from the repository into the cache for the current symbol.
    /// </summary>
    private async Task LoadAsync()
    {
        var trades = await _repository.GetTradesAsync(_symbol, _lifetime.ApplicationStopping);

        foreach (var trade in trades)
        {
            Apply(trade);
        }
    }

    /// <summary>
    /// Saves the trades to the cache and notifies all pending reactive pollers.
    /// </summary>
    public Task SetTradeAsync(AccountTrade trade)
    {
        Guard.IsNotNull(trade, nameof(trade));

        Apply(trade);

        return Task.CompletedTask;
    }

    public Task SetTradesAsync(IEnumerable<AccountTrade> trades)
    {
        Guard.IsNotNull(trades, nameof(trades));

        foreach (var trade in trades)
        {
            Apply(trade);
        }

        return Task.CompletedTask;
    }

    private void Apply(AccountTrade trade)
    {
        // validate
        Guard.IsEqualTo(trade.Symbol, _symbol, nameof(trade.Symbol));

        // remove old item to allow an update
        if (_trades.Remove(trade) && !Unindex(trade))
        {
            ThrowHelper.ThrowInvalidOperationException($"Failed to unindex trade ('{trade.Symbol}','{trade.Id}')");
        }

        _trades.Add(trade);

        Index(trade);
        Complete();
    }

    private void Index(AccountTrade trade)
    {
        _tradeByTradeId[trade.OrderId] = trade;
        _serialByTrade[trade] = ++_serial;
        _tradeBySerial[_serial] = trade;
    }

    private bool Unindex(AccountTrade trade)
    {
        return
            _tradeByTradeId.Remove(trade.OrderId) &&
            _serialByTrade.Remove(trade, out var serial) &&
            _tradeBySerial.Remove(serial);
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
            completion.SetResult(new ReactiveResult(_version, _serial, _trades.ToImmutable()));
        }
        else
        {
            // complete on changes only
            var builder = ImmutableSortedSet.CreateBuilder(AccountTrade.KeyComparer);

            for (var s = fromSerial; s <= _serial; s++)
            {
                if (_tradeBySerial.TryGetValue(s, out var trade))
                {
                    builder.Add(trade);
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