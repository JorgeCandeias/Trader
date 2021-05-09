using AutoMapper;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Concurrent;
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
    internal class SqlTradingRepository : ITradingRepository
    {
        private readonly SqlTradingRepositoryOptions _options;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        public SqlTradingRepository(IOptions<SqlTradingRepositoryOptions> options, ILogger<SqlTradingRepository> logger, IMapper mapper)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // cache the retry policy to create less garbage
            _retryPolicy = Policy
                .Handle<SqlException>()
                .RetryAsync(_options.RetryCount, (ex, retry) =>
                {
                    _logger.LogError(ex,
                        "{Name} handled exception and will retry ({Retry}/{Total})",
                        Name, retry, _options.RetryCount);
                });
        }

        private static string Name => nameof(SqlTradingRepository);

        private readonly AsyncRetryPolicy _retryPolicy;

        private readonly ConcurrentDictionary<string, int> _symbolLookup = new(StringComparer.OrdinalIgnoreCase);

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

        public async Task<OrderQueryResult> GetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            var entity = await connection
                .QuerySingleOrDefaultAsync<OrderEntity>(
                    new CommandDefinition(
                        "[dbo].[GetOrder]",
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

            return _mapper.Map<OrderQueryResult>(entity);
        }

        public async Task<OrderQueryResult?> GetLatestOrderBySideAsync(string symbol, OrderSide side, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            var entity = await connection
                .QuerySingleOrDefaultAsync<OrderEntity>(
                    new CommandDefinition(
                        "[dbo].[GetLatestOrderBySide]",
                        new
                        {
                            Symbol = symbol,
                            Side = _mapper.Map<int>(side)
                        },
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                        cancellationToken))
                .ConfigureAwait(false);

            return _mapper.Map<OrderQueryResult>(entity);
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

        public async Task<ImmutableSortedOrderSet> GetSignificantCompletedOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            var entities = await connection
                .QueryAsync<OrderEntity>(
                    new CommandDefinition(
                        "[dbo].[GetSignificantCompletedOrders]",
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

            var symbolId = await GetOrAddSymbolAsync(result.Symbol, cancellationToken).ConfigureAwait(false);

            var entity = _mapper.Map<CancelOrderEntity>(result, options =>
            {
                options.Items[nameof(CancelOrderEntity.SymbolId)] = symbolId;
            });

            using var connection = new SqlConnection(_options.ConnectionString);

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

            // get the cached ids for the incoming symbols
            var ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var order in orders)
            {
                // check the local fast dictionary
                if (!ids.ContainsKey(order.Symbol))
                {
                    // defer to the slower shared dictionary and database
                    ids.Add(order.Symbol, await GetOrAddSymbolAsync(order.Symbol, cancellationToken).ConfigureAwait(false));
                }
            }

            // pass the fast lookup to mapper so it knows how to populate the symbol ids
            var entities = _mapper.Map<IEnumerable<OrderTableParameterEntity>>(orders, options =>
            {
                options.Items[nameof(OrderTableParameterEntity.SymbolId)] = ids;
            });

            using var connection = new SqlConnection(_options.ConnectionString);

            await Policy
                .Handle<SqlException>()
                .RetryAsync(_options.RetryCount, (ex, retry) =>
                {
                    _logger.LogError(ex,
                        "{Name} handled exception while calling [dbo].[SetOrders] and will retry ({Retry}/{Total})",
                        Name, retry, _options.RetryCount);
                })
                .ExecuteAsync(ct => connection
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
                        ct)),
                        cancellationToken,
                        false)
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

            // get the cached ids for the incoming symbols
            var ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var trade in trades)
            {
                // check the local fast dictionary
                if (!ids.ContainsKey(trade.Symbol))
                {
                    // defer to the slower shared dictionary and database
                    ids.Add(trade.Symbol, await GetOrAddSymbolAsync(trade.Symbol, cancellationToken).ConfigureAwait(false));
                }
            }

            // pass the fast lookup to mapper so it knows how to populate the symbol ids
            var entities = _mapper.Map<IEnumerable<TradeTableParameterEntity>>(trades, options =>
            {
                options.Items[nameof(TradeTableParameterEntity.SymbolId)] = ids;
            });

            using var connection = new SqlConnection(_options.ConnectionString);

            await _retryPolicy
                .ExecuteAsync(ct => connection
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
                            ct)),
                        cancellationToken,
                        false)
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

            await _retryPolicy
                .ExecuteAsync(ct => connection
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
                        ct)),
                        cancellationToken,
                        false)
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

        private async ValueTask<int> GetOrAddSymbolAsync(string symbol, CancellationToken cancellation)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            if (_symbolLookup.TryGetValue(symbol, out var id))
            {
                return id;
            }

            using var connection = new SqlConnection(_options.ConnectionString);

            var parameters = new DynamicParameters();
            parameters.Add("Name", symbol, DbType.String, ParameterDirection.Input);
            parameters.Add("Id", null, DbType.Int32, ParameterDirection.Output);

            await connection
                .ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        "[dbo].[GetOrAddSymbol]",
                        parameters,
                        null,
                        _options.CommandTimeoutAsInteger,
                        CommandType.StoredProcedure,
                        CommandFlags.Buffered,
                        cancellation))
                .ConfigureAwait(false);

            id = parameters.Get<int>("Id");

            _symbolLookup.TryAdd(symbol, id);

            return id;
        }

        public async Task SetTickersAsync(IEnumerable<MiniTicker> tickers, CancellationToken cancellationToken = default)
        {
            _ = tickers ?? throw new ArgumentNullException(nameof(tickers));

            // get the cached ids for the incoming symbols
            var symbolIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var ticker in tickers)
            {
                // check the local fast dictionary
                if (!symbolIds.ContainsKey(ticker.Symbol))
                {
                    // defer to the slower shared dictionary and database
                    symbolIds.Add(ticker.Symbol, await GetOrAddSymbolAsync(ticker.Symbol, cancellationToken).ConfigureAwait(false));
                }
            }

            var entities = _mapper.Map<IEnumerable<TickerTableParameterEntity>>(tickers, options =>
            {
                options.Items[nameof(TickerTableParameterEntity.SymbolId)] = symbolIds;
            });

            using var connection = new SqlConnection(_options.ConnectionString);

            await _retryPolicy
                .ExecuteAsync(ct => connection
                    .ExecuteAsync(
                        new CommandDefinition(
                            "[dbo].[SetTickers]",
                            new
                            {
                                Tickers = entities.AsSqlDataRecords().AsTableValuedParameter("[dbo].[TickerTableParameter]")
                            },
                            null,
                            _options.CommandTimeoutAsInteger,
                            CommandType.StoredProcedure,
                            CommandFlags.Buffered,
                        ct)),
                        cancellationToken,
                        false)
                .ConfigureAwait(false);
        }

        public async Task<MiniTicker> GetTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            _ = symbol ?? throw new ArgumentNullException(nameof(symbol));

            using var connection = new SqlConnection(_options.ConnectionString);

            var entity = await connection
                .QuerySingleOrDefaultAsync<TickerEntity>(
                    new CommandDefinition(
                        "[dbo].[GetTicker]",
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

            return _mapper.Map<MiniTicker>(entity);
        }
    }
}