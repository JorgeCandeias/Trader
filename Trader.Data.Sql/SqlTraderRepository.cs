using AutoMapper;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Data.Sql.Models;
using Trader.Models;
using Trader.Models.Collections;

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

        public async Task<long> GetMaxTradeIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            return await connection
                .ExecuteScalarAsync<long>(
                    new CommandDefinition(
                        "[dbo].[GetMaxTradeId]",
                        new
                        {
                            Symbol = symbol
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public async Task<long> GetMinTransientOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            return await connection
                .ExecuteScalarAsync<long>(
                    new CommandDefinition(
                        "[dbo].[GetMinTransientOrderId]",
                        new
                        {
                            Symbol = symbol
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public async Task<ImmutableSortedOrderSet> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            var entities = await connection
                .QueryAsync<OrderEntity>(
                    new CommandDefinition(
                        "[dbo].[GetOrders]",
                        new
                        {
                            Symbol = symbol
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);

            return _mapper.Map<ImmutableSortedOrderSet>(entities);
        }

        public async Task<ImmutableSortedOrderSet> GetTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            var entities = await connection
                .QueryAsync<OrderEntity>(
                    new CommandDefinition(
                        "[dbo].[GetTransientOrdersBySide]",
                        new
                        {
                            Symbol = symbol,
                            Side = orderSide
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);

            return _mapper.Map<ImmutableSortedOrderSet>(entities);
        }

        public async Task<ImmutableSortedOrderSet> GetNonSignificantTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            var entities = await connection
                .QueryAsync<OrderEntity>(
                    new CommandDefinition(
                        "[dbo].[GetNonSignificantTransientOrdersBySide]",
                        new
                        {
                            Symbol = symbol,
                            Side = orderSide
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);

            return _mapper.Map<ImmutableSortedOrderSet>(entities);
        }

        public async Task<long> GetLastPagedOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            return await connection
                .ExecuteScalarAsync<long>(
                    new CommandDefinition(
                        "[dbo].[GetPagedOrder]",
                        new
                        {
                            Symbol = symbol
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public async Task SetLastPagedOrderIdAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            await connection
                .ExecuteAsync(
                    new CommandDefinition(
                        "[dbo].[SetPagedOrder]",
                        new
                        {
                            Symbol = symbol,
                            OrderId = orderId
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public async Task<long> GetLastPagedTradeIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            return await connection
                .ExecuteScalarAsync<long>(
                    new CommandDefinition(
                        "[dbo].[GetPagedTrade]",
                        new
                        {
                            Symbol = symbol
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public async Task SetLastPagedTradeIdAsync(string symbol, long tradeId, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            await connection
                .ExecuteAsync(
                    new CommandDefinition(
                        "[dbo].[SetPagedTrade]",
                        new
                        {
                            Symbol = symbol,
                            TradeId = tradeId
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            _ = order ?? throw new ArgumentNullException(nameof(order));

            return SetOrdersAsync(Enumerable.Repeat(order, 1), cancellationToken);
        }

        public async Task SetOrderAsync(CancelStandardOrderResult result, CancellationToken cancellationToken = default)
        {
            _ = result ?? throw new ArgumentNullException(nameof(result));

            using var connection = new SqlConnection(_options.ConnectionString);

            var entity = _mapper.Map<CancelOrderEntity>(result);

            await connection
                .ExecuteAsync(
                    new CommandDefinition(
                        "[dbo].[CancelOrder]",
                        entity,
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public async Task SetOrderAsync(OrderResult result, decimal stopPrice = 0m, decimal icebergQuantity = 0m, decimal originalQuoteOrderQuantity = 0m, CancellationToken cancellationToken = default)
        {
            _ = result ?? throw new ArgumentNullException(nameof(result));

            using var connection = new SqlConnection(_options.ConnectionString);

            var order = _mapper.Map<OrderQueryResult>(result, options =>
            {
                options.Items[nameof(OrderQueryResult.StopPrice)] = stopPrice;
                options.Items[nameof(OrderQueryResult.IcebergQuantity)] = icebergQuantity;
                options.Items[nameof(OrderQueryResult.OriginalQuoteOrderQuantity)] = originalQuoteOrderQuantity;
            });

            await SetOrderAsync(order, cancellationToken).ConfigureAwait(false);
        }

        public async Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default)
        {
            _ = orders ?? throw new ArgumentNullException(nameof(orders));

            using var connection = new SqlConnection(_options.ConnectionString);

            var entities = _mapper.Map<IEnumerable<OrderTableParameterEntity>>(orders);

            await connection
                .ExecuteAsync(
                    new CommandDefinition(
                        "[dbo].[SetOrders]",
                        new
                        {
                            Orders = entities.AsSqlDataRecords().AsTableValuedParameter("[dbo].[OrderTableParameter]")
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default)
        {
            _ = trade ?? throw new ArgumentNullException(nameof(trade));

            return SetTradesAsync(Enumerable.Repeat(trade, 1), cancellationToken);
        }

        public async Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
        {
            _ = trades ?? throw new ArgumentNullException(nameof(trades));

            using var connection = new SqlConnection(_options.ConnectionString);

            var entities = _mapper.Map<IEnumerable<TradeTableParameterEntity>>(trades);

            await connection
                .ExecuteAsync(
                    new CommandDefinition(
                        "[dbo].[SetTrades]",
                        new
                        {
                            Trades = entities.AsSqlDataRecords().AsTableValuedParameter("[dbo].[TradeTableParameter]")
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public async Task<ImmutableSortedTradeSet> GetTradesAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            var result = await connection
                .QueryAsync<TradeEntity>(
                    new CommandDefinition(
                        "[dbo].[GetTrades]",
                        new
                        {
                            Symbol = symbol
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);

            return _mapper.Map<ImmutableSortedTradeSet>(result);
        }

        public Task SetBalancesAsync(AccountInfo accountInfo, CancellationToken cancellationToken = default)
        {
            _ = accountInfo ?? throw new ArgumentNullException(nameof(accountInfo));

            var balances = _mapper.Map<IEnumerable<Balance>>(accountInfo);

            return SetBalancesAsync(balances, cancellationToken);
        }

        public async Task SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default)
        {
            _ = balances ?? throw new ArgumentNullException(nameof(balances));

            using var connection = new SqlConnection(_options.ConnectionString);

            var entities = _mapper.Map<IEnumerable<BalanceTableParameterEntity>>(balances);

            await connection
                .ExecuteAsync(
                    new CommandDefinition(
                        "[dbo].[SetBalances]",
                        new
                        {
                            Balances = entities.AsSqlDataRecords().AsTableValuedParameter("[dbo].[BalanceTableParameter]")
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                    cancellationToken))
                .ConfigureAwait(false);
        }

        public async Task<Balance> GetBalanceAsync(string asset, CancellationToken cancellationToken = default)
        {
            _ = asset ?? throw new ArgumentNullException(nameof(asset));

            using var connection = new SqlConnection(_options.ConnectionString);

            var entity = await connection
                .QuerySingleOrDefaultAsync<BalanceEntity>(
                    new CommandDefinition(
                        "[dbo].[GetBalance]",
                        new
                        {
                            Asset = asset
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                        cancellationToken))
                .ConfigureAwait(false);

            return _mapper.Map<Balance>(entity);
        }
    }
}