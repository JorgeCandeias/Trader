using Microsoft.Extensions.Logging;
using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

                LogAssetQuantity(TypeName, significant.Symbol.Name, quantity);

                var total = significant.Orders.Sum(x => x.Price * x.ExecutedQuantity);

                LogAssetCost(TypeName, significant.Symbol.Name, total);

                var now = quantity * ticker.ClosePrice;

                _logger.LogInformation(
                    "{Type} {Name} reports Present Value = {Value:F8}",
                    TypeName, significant.Symbol.Name, now);

                var uPnL = now - total;

                _logger.LogInformation(
                    "{Type} {Name} reports Unrealized PnL = {Value:F8} ({Ratio:P8})",
                    TypeName, significant.Symbol.Name, uPnL, total == 0m ? 0m : uPnL / total);

                var rPnl = significant.ProfitEvents.Sum(x => x.Profit);

                _logger.LogInformation(
                    "{Type} {Name} reports Realized PnL = {Value:F8}",
                    TypeName, significant.Symbol.Name, rPnl, rPnl);

                var pPnl = uPnL + rPnl;

                _logger.LogInformation(
                    "{Type} {Name} reports Present PnL = {Value:F8}",
                    TypeName, significant.Symbol.Name, pPnl);

                // todo: we need past price history to convert non-quote asset comissions into quote asset comissions for accurate reporting here
                var commissions = significant.CommissionEvents.Where(x => x.Asset == significant.Symbol.QuoteAsset).Sum(x => x.Commission);

                _logger.LogInformation(
                    "{Type} {Name} reports Quote Commissions = {Value:F8}",
                    TypeName, significant.Symbol.Name, commissions);

                var aPnL = pPnl - commissions;

                _logger.LogInformation(
                    "{Type} {Name} reports Adjusted PnL = {Value:F8}",
                    TypeName, significant.Symbol.Name, aPnL);
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

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Asset Quantity = {Quantity:F8}")]
        private partial void LogAssetQuantity(string type, string name, decimal quantity);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Asset Cost = {Total:F8}")]
        private partial void LogAssetCost(string type, string name, decimal total);

        #endregion Logging
    }
}