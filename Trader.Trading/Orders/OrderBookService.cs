using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Trading.Orders
{
    internal class OrderBookService : IOrderBookService, IHostedService
    {
        private readonly OrderBookServiceOptions _options;
        private readonly ILogger _logger;

        public OrderBookService(string name, IOptionsSnapshot<OrderBookServiceOptions> options, ILogger<OrderBookService> logger)
        {
            _options = options.Get(name) ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Symbol => _options.Symbol;

        private static string Name => nameof(OrderBookService);

        #region Hosted Service

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "{Name} {Symbol} starting",
                Name, _options.Symbol);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "{Name} {Symbol} stopped",
                Name, _options.Symbol);

            return Task.CompletedTask;
        }

        #endregion Hosted Service
    }
}