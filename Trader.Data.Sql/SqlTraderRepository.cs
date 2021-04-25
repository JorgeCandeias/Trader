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

        public Task<long> GetMaxTradeIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return GetMaxTradeIdInnerAsync(symbol, cancellationToken);
        }

        private async Task<long> GetMaxTradeIdInnerAsync(string symbol, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_options.ConnectionString);

            return await connection
                .ExecuteScalarAsync<long>(
                    new CommandDefinition(
                        "[dbo].[GetMaxTradeId]",
                        new { symbol },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                        cancellationToken))
                .ConfigureAwait(false);
        }

        public Task<long> GetMinTransientOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return GetMinTransientOrderIdInnerAsync(symbol, cancellationToken);
        }

        private async Task<long> GetMinTransientOrderIdInnerAsync(string symbol, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_options.ConnectionString);

            return await connection
                .ExecuteScalarAsync<long>(
                    new CommandDefinition(
                        "[dbo].[GetMinTransientOrderId]",
                        new { symbol },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                        cancellationToken))
                .ConfigureAwait(false);
        }

        public Task<OrderQueryResult> GetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return GetOrderInnerAsync(symbol, orderId, cancellationToken);
        }

        private async Task<OrderQueryResult> GetOrderInnerAsync(string symbol, long orderId, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_options.ConnectionString);

            var entity = await connection
                .QuerySingleOrDefaultAsync<OrderEntity>(new CommandDefinition(
                    "[dbo].[GetOrder]",
                    new
                    {
                        symbol,
                        orderId
                    },
                    null,
                    _options.CommandTimeoutAsInteger,
                    CommandType.StoredProcedure,
                    CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);

            return _mapper.Map<OrderQueryResult>(entity);
        }

        public Task<SortedOrderSet> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return GetOrdersInnerAsync(symbol, cancellationToken);
        }

        private async Task<SortedOrderSet> GetOrdersInnerAsync(string symbol, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_options.ConnectionString);

            var entities = await connection
                .QueryAsync<OrderEntity>(new CommandDefinition(
                    "[dbo].[GetOrders]",
                    new
                    {
                        symbol
                    },
                    null,
                    _options.CommandTimeoutAsInteger,
                    CommandType.StoredProcedure,
                    CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);

            return _mapper.Map<SortedOrderSet>(entities);
        }

        public Task<SortedOrderSet> GetTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return GetTransientOrdersBySideInnerAsync(symbol, orderSide, cancellationToken);
        }

        private async Task<SortedOrderSet> GetTransientOrdersBySideInnerAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_options.ConnectionString);

            var entities = await connection.QueryAsync<OrderEntity>(
                new CommandDefinition(
                    "[dbo].[GetTransientOrdersBySide]",
                    new
                    {
                        Symbol = symbol,
                        Side = orderSide
                    },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            return _mapper.Map<SortedOrderSet>(entities);
        }

        public Task<SortedOrderSet> GetNonSignificantTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return GetNonSignificantTransientOrdersBySideInnerAsync(symbol, orderSide, cancellationToken);
        }

        private async Task<SortedOrderSet> GetNonSignificantTransientOrdersBySideInnerAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_options.ConnectionString);

            var entities = await connection.QueryAsync<OrderEntity>(
                new CommandDefinition(
                    "[dbo].[GetNonSignificantTransientOrdersBySide]",
                    new
                    {
                        Symbol = symbol,
                        Side = orderSide
                    },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            return _mapper.Map<SortedOrderSet>(entities);
        }

        public Task<long> GetLastPagedOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return GetLastPagedOrderIdInnerAsync(symbol, cancellationToken);
        }

        private async Task<long> GetLastPagedOrderIdInnerAsync(string symbol, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_options.ConnectionString);

            return await connection
                .ExecuteScalarAsync<long>(new CommandDefinition(
                    "[dbo].[GetPagedOrder]",
                    new
                    {
                        symbol
                    },
                    null,
                    _options.CommandTimeoutAsInteger,
                    CommandType.StoredProcedure,
                    CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public Task SetLastPagedOrderIdAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return SetLastPagedOrderIdInnerAsync(symbol, orderId, cancellationToken);
        }

        private async Task SetLastPagedOrderIdInnerAsync(string symbol, long orderId, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_options.ConnectionString);

            await connection
                .ExecuteAsync(new CommandDefinition(
                    "[dbo].[SetPagedOrder]",
                    new
                    {
                        symbol,
                        orderId
                    },
                    null,
                    _options.CommandTimeoutAsInteger,
                    CommandType.Text,
                    CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            _ = order ?? throw new ArgumentNullException(nameof(order));

            return SetOrderInnerAsync(order, cancellationToken);
        }

        public Task SetOrderAsync(CancelStandardOrderResult result, CancellationToken cancellationToken = default)
        {
            _ = result ?? throw new ArgumentNullException(nameof(result));

            return SetOrderInnerAsync(result, cancellationToken);
        }

        public Task SetOrderAsync(OrderResult result, CancellationToken cancellationToken = default)
        {
            _ = result ?? throw new ArgumentNullException(nameof(result));

            return SetOrderInnerAsync(result, cancellationToken);
        }

        private async Task SetOrderInnerAsync(OrderQueryResult order, CancellationToken cancellationToken)
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

        private async Task SetOrderInnerAsync(CancelStandardOrderResult result, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_options.ConnectionString);

            var order = _mapper.Map<OrderQueryResult>(result);

            await SetOrderAsync(order, cancellationToken);
        }

        private async Task SetOrderInnerAsync(OrderResult result, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_options.ConnectionString);

            var order = _mapper.Map<OrderQueryResult>(result);

            await SetOrderAsync(order, cancellationToken);
        }

        public Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default)
        {
            _ = orders ?? throw new ArgumentNullException(nameof(orders));

            return SetOrdersInnerAsync(orders, cancellationToken);
        }

        private async Task SetOrdersInnerAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken)
        {
            foreach (var order in orders)
            {
                await SetOrderAsync(order, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default)
        {
            _ = trade ?? throw new ArgumentNullException(nameof(trade));

            return SetTradeInnerAsync(trade, cancellationToken);
        }

        private async Task SetTradeInnerAsync(AccountTrade trade, CancellationToken cancellationToken)
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

            return SetTradesInnerAsync(trades, cancellationToken);
        }

        private async Task SetTradesInnerAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken)
        {
            foreach (var trade in trades)
            {
                await SetTradeAsync(trade, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task<SortedTradeSet> GetTradesAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            return GetTradesInnerAsync(symbol, cancellationToken);
        }

        private async Task<SortedTradeSet> GetTradesInnerAsync(string symbol, CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(_options.ConnectionString);

            var result = await connection.QueryAsync<TradeEntity>(
                new CommandDefinition(
                    "[dbo].[GetTrades]",
                    new { symbol },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            return _mapper.Map<SortedTradeSet>(result);
        }
    }
}