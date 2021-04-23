using AutoMapper;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Data.Sql
{
    internal class SqlTraderRepository : ITraderRepository
    {
        private readonly SqlTraderRepositoryOptions _options;
        private readonly IMapper _mapper;

        public SqlTraderRepository(IOptions<SqlTraderRepositoryOptions> options, IMapper mapper)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
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
            _ = order ?? throw new ArgumentNullException(nameof(order));

            return InnerSetOrderAsync(order, cancellationToken);
        }

        private async Task InnerSetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_options.ConnectionString);

            var entity = _mapper.Map<OrderEntity>(order);

            await connection
                .ExecuteAsync(new CommandDefinition(
                    "[dbo].[SetOrder]",
                    entity,
                    null,
                    _options.CommandTimeoutAsInteger,
                    CommandType.StoredProcedure,
                    CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default)
        {
            _ = orders ?? throw new ArgumentNullException(nameof(orders));

            return InnerSetOrdersAsync(orders, cancellationToken);
        }

        private async Task InnerSetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken)
        {
            foreach (var order in orders)
            {
                await SetOrderAsync(order, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default)
        {
            _ = trade ?? throw new ArgumentNullException(nameof(trade));

            return InnerSetTradeAsync(trade, cancellationToken);
        }

        private async Task InnerSetTradeAsync(AccountTrade trade, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_options.ConnectionString);

            var entity = _mapper.Map<TradeEntity>(trade);

            await connection
                .ExecuteAsync(new CommandDefinition(
                    "[dbo].[SetTrade]",
                    entity,
                    null,
                    _options.CommandTimeoutAsInteger,
                    CommandType.StoredProcedure,
                    CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
        {
            _ = trades ?? throw new ArgumentNullException(nameof(trades));

            return InnerSetTradesAsync(trades, cancellationToken);
        }

        private async Task InnerSetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken)
        {
            foreach (var trade in trades)
            {
                await SetTradeAsync(trade, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}