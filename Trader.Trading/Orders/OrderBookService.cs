using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Trading.Orders
{
    internal class OrderBookService : IOrderBookService, IHostedService
    {
        private readonly OrderBookServiceOptions _options;

        public OrderBookService(string name, IOptionsSnapshot<OrderBookServiceOptions> options)
        {
            _options = options.Get(name) ?? throw new ArgumentNullException(nameof(options));
        }

        public string Symbol => _options.Symbol;

        #region Hosted Service

        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion Hosted Service
    }
}