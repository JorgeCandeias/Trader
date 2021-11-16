using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Outcompute.Trader.Trading.Providers.Exchange;

internal sealed partial class ExchangeInfoProvider : BackgroundService, IExchangeInfoProvider
{
    private readonly ExchangeInfoOptions _options;
    private readonly ILogger _logger;
    private readonly IGrainFactory _factory;

    public ExchangeInfoProvider(IOptions<ExchangeInfoOptions> options, ILogger<ExchangeInfoProvider> logger, IGrainFactory factory)
    {
        _options = options.Value;
        _logger = logger;
        _factory = factory;
    }

    private const string TypeName = nameof(ExchangeInfoProvider);

    private readonly ConcurrentDictionary<string, Symbol> _symbols = new();

    private ExchangeInfo _info = ExchangeInfo.Empty;
    private Guid _version;

    public ExchangeInfo GetExchangeInfo()
    {
        return _info;
    }

    public Symbol? TryGetSymbol(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
        {
            return symbol;
        }

        return null;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        LogStarting(TypeName);

        var watch = Stopwatch.StartNew();

        (_info, _version) = await _factory.GetExchangeInfoGrain().GetExchangeInfoAsync();

        Index();

        LogStarted(TypeName, watch.ElapsedMilliseconds);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_options.PropagationPeriod, stoppingToken).ConfigureAwait(false);

            try
            {
                var result = await _factory.GetExchangeInfoGrain().TryGetExchangeInfoAsync(_version);

                if (result.Value is not null)
                {
                    _info = result.Value;
                    _version = result.Version;

                    Index();
                }
            }
            catch (Exception ex)
            {
                LogRefreshError(ex, TypeName);
            }
        }
    }

    private void Index()
    {
        foreach (var symbol in _info.Symbols)
        {
            _symbols[symbol.Name] = symbol;
        }
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{Type} starting...")]
    private partial void LogStarting(string type);

    [LoggerMessage(1, LogLevel.Information, "{Type} started in {ElapsedMs}ms")]
    private partial void LogStarted(string type, long elapsedMs);

    [LoggerMessage(2, LogLevel.Error, "{Type} handled error while refreshing exchange information")]
    private partial void LogRefreshError(Exception ex, string type);

    #endregion Logging
}