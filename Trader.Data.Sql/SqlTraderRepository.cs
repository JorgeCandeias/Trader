using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Data.Sql
{
    internal class SqlTraderRepository : ITraderRepository
    {
        private readonly SqlTraderRepositoryOptions _options;

        public SqlTraderRepository(IOptions<SqlTraderRepositoryOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task ApplyAsync(CancelStandardOrderResult result, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task ApplyAsync(OrderResult result, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<long> GetLastPagedOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<long> GetMaxOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<long> GetMaxTradeIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<long> GetMinTransientOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<OrderQueryResult> GetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<SortedOrderSet> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<SortedTradeSet> GetTradesAsync(string symbol, long? orderId = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<SortedOrderSet> GetTransientOrdersAsync(string symbol, OrderSide? orderSide = null, bool? significant = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task SetLastPagedOrderIdAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}