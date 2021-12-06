using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Algorithms.Positions;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators;

internal partial class AlgoContextAutoPositionsConfigurator : IAlgoContextConfigurator<AlgoContext>
{
    private readonly IOptionsMonitor<AlgoOptions> _options;
    private readonly ILogger _logger;
    private readonly IAutoPositionResolver _resolver;

    public AlgoContextAutoPositionsConfigurator(IOptionsMonitor<AlgoOptions> monitor, ILogger<AlgoContextAutoPositionsConfigurator> logger, IAutoPositionResolver resolver)
    {
        _options = monitor;
        _logger = logger;
        _resolver = resolver;
    }

    private const string TypeName = nameof(AlgoContextAutoPositionsConfigurator);

    public ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
    {
        var options = _options.Get(name);

        foreach (var symbol in context.Symbols)
        {
            var data = context.Data[symbol.Name];

            try
            {
                data.AutoPosition = _resolver.Resolve(symbol, data.Orders.Filled, data.Trades, options.StartTime);
            }
            catch (AutoPositionResolverException ex)
            {
                LogError(ex, TypeName, name);
                data.Exceptions.Add(ex);
            }
        }

        return ValueTask.CompletedTask;
    }

    #region Logging

    [LoggerMessage(1, LogLevel.Error, "{Type} caught exception while configuring positions for algo {Name}")]
    private partial void LogError(Exception ex, string type, string name);

    #endregion Logging
}