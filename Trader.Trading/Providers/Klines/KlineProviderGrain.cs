using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using OrleansDashboard;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System.Buffers;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Providers.Klines;

[Reentrant]
internal class KlineProviderGrain : Grain, IKlineProviderGrain
{
    private readonly KlineProviderOptions _options;
    private readonly ReactiveOptions _reactive;
    private readonly ITradingRepository _repository;
    private readonly ISystemClock _clock;
    private readonly IHostApplicationLifetime _lifetime;

    public KlineProviderGrain(IOptions<KlineProviderOptions> options, IOptions<ReactiveOptions> reactive, ITradingRepository repository, ISystemClock clock, IHostApplicationLifetime lifetime)
    {
        _options = options.Value;
        _reactive = reactive.Value;
        _repository = repository;
        _clock = clock;
        _lifetime = lifetime;
    }

    /// <summary>
    /// The symbol that this grain is responsible for.
    /// </summary>
    internal string _symbol = null!;

    /// <summary>
    /// The interval that this grain instance is reponsible for.
    /// </summary>
    internal KlineInterval _interval;

    /// <summary>
    /// The current version.
    /// </summary>
    private Guid _version = Guid.NewGuid();

    /// <summary>
    /// The current change serial number;
    /// </summary>
    private int _serial;

    /// <summary>
    /// Holds the kline cache in a form that is mutable but still convertible to immutable upon request with low overhead.
    /// </summary>
    private readonly ImmutableSortedSet<Kline>.Builder _klines = ImmutableSortedSet.CreateBuilder(KlineComparer.Key);

    /// <summary>
    /// Indexes klines by open time to speed up requests for a single order.
    /// </summary>
    private readonly Dictionary<DateTime, Kline> _klineByOpenTime = new();

    /// <summary>
    /// Assigns a unique serial number to each kline.
    /// </summary>
    private readonly Dictionary<Kline, int> _serialByKline = new(KlineComparer.Key);

    /// <summary>
    /// Indexes each kline by it serial number.
    /// </summary>
    private readonly Dictionary<int, Kline> _klineBySerial = new();

    /// <summary>
    /// Tracks all reactive caching requests.
    /// </summary>
    private readonly Dictionary<(Guid Version, int FromSerial), TaskCompletionSource<ReactiveResult?>> _completions = new();

    public override async Task OnActivateAsync()
    {
        (_symbol, _interval) = this.GetPrimaryKeys();

        await LoadAsync();

        RegisterTimer(_ => CleanupAsync(), null, _options.CleanupPeriod, _options.CleanupPeriod);

        await base.OnActivateAsync();
    }

    public ValueTask<Kline?> TryGetKlineAsync(DateTime openTime)
    {
        return new(_klineByOpenTime.GetValueOrDefault(openTime));
    }

    public ValueTask<ReactiveResult> GetKlinesAsync()
    {
        return new(CreateSnapshot());
    }

    [NoProfiling]
    public ValueTask<ReactiveResult?> TryWaitForKlinesAsync(Guid version, int fromSerial)
    {
        // if the versions differ then return the entire data set
        if (version != _version)
        {
            return new(CreateSnapshot());
        }

        // fulfill the request now if possible
        if (_serial >= fromSerial)
        {
            return new(CreateUpdate(fromSerial));
        }

        // otherwise let the request wait for more data
        return new ValueTask<ReactiveResult?>(GetOrCreateCompletionTask(version, fromSerial).WithDefaultOnTimeout(null, _reactive.ReactivePollingTimeout, _lifetime.ApplicationStopping));
    }

    private ReactiveResult CreateSnapshot()
    {
        return new ReactiveResult(_version, _serial, _klines.ToImmutable().AsKlineCollection());
    }

    private ReactiveResult CreateUpdate(int fromSerial)
    {
        var builder = ImmutableSortedSet.CreateBuilder(KlineComparer.Key);

        for (var i = fromSerial; i <= _serial; i++)
        {
            if (_klineBySerial.TryGetValue(i, out var kline))
            {
                builder.Add(kline);
            }
        }

        return new ReactiveResult(_version, _serial, builder.ToImmutable().AsKlineCollection());
    }

    private async ValueTask LoadAsync()
    {
        var result = await _repository.GetKlinesAsync(_symbol, _interval, _clock.UtcNow.Subtract(_interval, _options.MaxCachedKlines + 1), _clock.UtcNow);

        foreach (var kline in result)
        {
            Apply(kline);
        }
    }

    public ValueTask SetKlineAsync(Kline item)
    {
        if (item.Symbol != _symbol) throw new ArgumentOutOfRangeException(nameof(item));
        if (item.Interval != _interval) throw new ArgumentOutOfRangeException(nameof(item));

        Apply(item);

        return ValueTask.CompletedTask;
    }

    public ValueTask SetKlinesAsync(IEnumerable<Kline> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));

        foreach (var item in items)
        {
            Apply(item);
        }

        return ValueTask.CompletedTask;
    }

    private void Apply(Kline item)
    {
        Remove(item);

        _klines.Add(item);

        Index(item);
        Complete();
    }

    private void Remove(Kline item)
    {
        if (_klines.Remove(item) && !Unindex(item))
        {
            throw new InvalidOperationException($"Failed to unindex kline ('{item.Symbol}','{item.Interval}','{item.OpenTime}')");
        }
    }

    private void Index(Kline item)
    {
        _klineByOpenTime[item.OpenTime] = item;
        _serialByKline[item] = ++_serial;
        _klineBySerial[_serial] = item;
    }

    private bool Unindex(Kline item)
    {
        return
            _klineByOpenTime.Remove(item.OpenTime) &&
            _serialByKline.Remove(item, out var serial) &&
            _klineBySerial.Remove(serial);
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
            completion.SetResult(CreateSnapshot());
        }
        else
        {
            // complete on changes only
            completion.SetResult(CreateUpdate(fromSerial));
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

    private Task CleanupAsync()
    {
        while (_klines.Count > _options.MaxCachedKlines)
        {
            Remove(_klines.Min!);
        }

        return Task.CompletedTask;
    }
}