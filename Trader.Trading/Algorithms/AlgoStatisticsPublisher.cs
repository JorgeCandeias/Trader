using Microsoft.Extensions.Logging;
using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal partial class AlgoStatisticsPublisher : IAlgoStatisticsPublisher
    {
        private readonly ILogger _logger;
        private readonly IGrainFactory _factory;

        public AlgoStatisticsPublisher(ILogger<AlgoStatisticsPublisher> logger, IGrainFactory factory)
        {
            _logger = logger;
            _factory = factory;
        }

        private static string TypeName => nameof(AlgoStatisticsPublisher);

        public Task PublishAsync(PositionDetails significant, MiniTicker ticker, CancellationToken cancellationToken = default)
        {
            if (significant is null) throw new ArgumentNullException(nameof(significant));
            if (ticker is null) throw new ArgumentNullException(nameof(ticker));

            return PublishCoreAsync(significant, ticker);
        }

        private async Task PublishCoreAsync(PositionDetails significant, MiniTicker ticker)
        {
            LogTicker(TypeName, ticker.Symbol, ticker.ClosePrice);

            if (significant.Orders.Count > 0)
            {
                var quantity = significant.Orders.Sum(x => x.ExecutedQuantity);

                var total = significant.Orders.Sum(x => x.Price * x.ExecutedQuantity);

                var avg = total / quantity;

                var now = quantity * ticker.ClosePrice;

                var uPnL = now - total;

                var rPnl = significant.ProfitEvents.Sum(x => x.Profit);

                var pPnl = uPnL + rPnl;

                // we need past price history to convert non-quote asset comissions into quote asset comissions for accurate reporting here
                var commissions = significant.CommissionEvents.Where(x => x.Asset == significant.Symbol.QuoteAsset).Sum(x => x.Commission);

                var aPnL = pPnl - commissions;

                LogStatistics(TypeName, significant.Symbol.Name, quantity, total, avg, now, uPnL, total == 0m ? 0m : uPnL / total, rPnl, pPnl, commissions, aPnL);
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

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Q = {Quantity:F8}, T = {Total:F8}, AVG = {AveragePrice:F8}, PV = {PV:F8}, UPNL = {UPNL:F8} ({UPNLRatio:P8}), RPNL = {RPNL:F8}, PPNL = {PPNL:F8}, QC = {QuoteCommissions:F8}, APNL = {APNL:F8}")]
        private partial void LogStatistics(string type, string name, decimal quantity, decimal total, decimal averagePrice, decimal pv, decimal upnl, decimal upnlRatio, decimal rpnl, decimal ppnl, decimal quoteCommissions, decimal apnl);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Asset Quantity = {Quantity:F8}")]
        private partial void LogAssetQuantity(string type, string name, decimal quantity);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Asset Cost = {Total:F8}")]
        private partial void LogAssetCost(string type, string name, decimal total);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Present Value = {Value:F8}")]
        private partial void LogPresentValue(string type, string name, decimal value);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Unrealized PnL = {Value:F8} ({Ratio:P8})")]
        private partial void LogUnrealizedPnL(string type, string name, decimal value, decimal ratio);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Realized PnL = {Value:F8}")]
        private partial void LogRealizedPnL(string type, string name, decimal value);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Present PnL = {Value:F8}")]
        private partial void LogPresentPnl(string type, string name, decimal value);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Quote Commissions = {Value:F8}")]
        private partial void LogQuoteCommissions(string type, string name, decimal value);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Adjusted PnL = {Value:F8}")]
        private partial void LogAdjustedPnL(string type, string name, decimal value);

        #endregion Logging
    }
}