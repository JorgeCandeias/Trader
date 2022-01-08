using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Algorithms.Positions;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms;

internal partial class AlgoStatisticsPublisher : IAlgoStatisticsPublisher
{
    private readonly ILogger _logger;
    private readonly IGrainFactory _factory;
    private readonly IBalanceProvider _balances;
    private readonly ISavingsProvider _savings;
    private readonly ISwapPoolProvider _swaps;

    public AlgoStatisticsPublisher(ILogger<AlgoStatisticsPublisher> logger, IGrainFactory factory, IBalanceProvider balances, ISavingsProvider savings, ISwapPoolProvider swaps)
    {
        _logger = logger;
        _factory = factory;
        _balances = balances;
        _savings = savings;
        _swaps = swaps;
    }

    private static string TypeName => nameof(AlgoStatisticsPublisher);

    public Task PublishAsync(AutoPosition significant, MiniTicker ticker, CancellationToken cancellationToken = default)
    {
        if (significant is null) throw new ArgumentNullException(nameof(significant));
        if (ticker is null) throw new ArgumentNullException(nameof(ticker));

        return PublishCoreAsync(significant, ticker, cancellationToken);
    }

    private async Task PublishCoreAsync(AutoPosition significant, MiniTicker ticker, CancellationToken cancellationToken)
    {
        if (significant.Positions.Count > 0)
        {
            var quantity = significant.Positions.Sum(x => x.Quantity);

            var total = significant.Positions.Sum(x => x.Price * x.Quantity);

            var avg = total / quantity;

            var now = quantity * ticker.ClosePrice;

            var uPnL = now - total;

            var rPnl = significant.ProfitEvents.Sum(x => x.Profit);

            var pPnl = uPnL + rPnl;

            // we need past price history to convert non-quote asset comissions into quote asset comissions for accurate reporting here
            var commissions = significant.CommissionEvents.Where(x => x.Asset == significant.Symbol.QuoteAsset).Sum(x => x.Commission);

            var aPnL = pPnl - commissions;

            LogStatistics(TypeName, significant.Symbol.Name, quantity, total, avg, now, uPnL, total == 0m ? 0m : uPnL / total, rPnl, pPnl, commissions, aPnL);

            var spotBalance = await _balances.GetBalanceOrZeroAsync(significant.Symbol.BaseAsset, cancellationToken);
            var savingsBalance = await _savings.GetBalanceOrZeroAsync(significant.Symbol.BaseAsset, cancellationToken);
            var swapBalance = await _swaps.GetBalanceAsync(significant.Symbol.BaseAsset, cancellationToken);
            var balance = spotBalance.Total + savingsBalance.TotalAmount + swapBalance.Total;

            if (balance < quantity)
            {
                LogFreeAmountLessThanPurchased(TypeName, significant.Symbol.Name, balance, significant.Symbol.BaseAsset, quantity);
            }
        }

        // this model is meant for rendering only and will get refactored at some point
        var profit = Profit.FromEvents(significant.Symbol, significant.ProfitEvents, significant.CommissionEvents, ticker.EventTime);

        await _factory
            .GetProfitAggregatorLocalGrain()
            .PublishAsync(profit)
            .ConfigureAwait(false);
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Ticker = {Ticker:F8}")]
    private partial void LogTicker(string type, string name, decimal ticker);

    [LoggerMessage(1, LogLevel.Information, "{Type} {Name} reports Q = {Quantity:F8}, T = {Total:F8}, AVG = {AveragePrice:F8}, PV = {PV:F8}, UPNL = {UPNL:F8} ({UPNLRatio:P8}), RPNL = {RPNL:F8}, PPNL = {PPNL:F8}, QC = {QuoteCommissions:F8}, APNL = {APNL:F8}")]
    private partial void LogStatistics(string type, string name, decimal quantity, decimal total, decimal averagePrice, decimal pv, decimal upnl, decimal upnlRatio, decimal rpnl, decimal ppnl, decimal quoteCommissions, decimal apnl);

    [LoggerMessage(2, LogLevel.Information, "{Type} {Name} reports Asset Quantity = {Quantity:F8}")]
    private partial void LogAssetQuantity(string type, string name, decimal quantity);

    [LoggerMessage(3, LogLevel.Information, "{Type} {Name} reports Asset Cost = {Total:F8}")]
    private partial void LogAssetCost(string type, string name, decimal total);

    [LoggerMessage(4, LogLevel.Information, "{Type} {Name} reports Present Value = {Value:F8}")]
    private partial void LogPresentValue(string type, string name, decimal value);

    [LoggerMessage(5, LogLevel.Information, "{Type} {Name} reports Unrealized PnL = {Value:F8} ({Ratio:P8})")]
    private partial void LogUnrealizedPnL(string type, string name, decimal value, decimal ratio);

    [LoggerMessage(6, LogLevel.Information, "{Type} {Name} reports Realized PnL = {Value:F8}")]
    private partial void LogRealizedPnL(string type, string name, decimal value);

    [LoggerMessage(7, LogLevel.Information, "{Type} {Name} reports Present PnL = {Value:F8}")]
    private partial void LogPresentPnl(string type, string name, decimal value);

    [LoggerMessage(8, LogLevel.Information, "{Type} {Name} reports Quote Commissions = {Value:F8}")]
    private partial void LogQuoteCommissions(string type, string name, decimal value);

    [LoggerMessage(9, LogLevel.Information, "{Type} {Name} reports Adjusted PnL = {Value:F8}")]
    private partial void LogAdjustedPnL(string type, string name, decimal value);

    [LoggerMessage(10, LogLevel.Warning, "{Type} {Name} reports total amount {Total:F8} {Asset} is less than purchased quantity of {Quantity:F8} {Asset}")]
    private partial void LogFreeAmountLessThanPurchased(string type, string name, decimal total, string asset, decimal quantity);

    [LoggerMessage(11, LogLevel.Information, "{Type} {Name} reports total amount {Free:F8} {Asset} meets or exceeds purchased quantity of {Quantity:F8} {Asset} by {Diff:F8} {Asset})")]
    private partial void LogFreeAmountExceedsPurchased(string type, string name, decimal free, string asset, decimal quantity, decimal diff);

    #endregion Logging
}