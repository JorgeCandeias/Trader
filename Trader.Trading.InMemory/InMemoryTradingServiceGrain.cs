using Orleans.Concurrency;
using Outcompute.Trader.Core.Time;
using static System.String;

namespace Outcompute.Trader.Trading.InMemory;

[Reentrant]
internal class InMemoryTradingServiceGrain : Grain, IInMemoryTradingServiceGrain
{
    private readonly ISystemClock _clock;

    public InMemoryTradingServiceGrain(ISystemClock clock)
    {
        _clock = clock;
    }

    private readonly Dictionary<string, Dictionary<string, SavingsBalance>> _positions = new();
    private readonly Dictionary<(string, SavingsRedemptionType), SavingsQuota> _quotas = new();
    private readonly Dictionary<string, Dictionary<long, OrderQueryResult>> _orders = new();
    private readonly Dictionary<string, long> _orderIds = new();
    private readonly Dictionary<string, Ticker> _tickers = new();
    private readonly Dictionary<string, Dictionary<long, AccountTrade>> _trades = new();
    private AccountInfo _account = AccountInfo.Empty;

    #region Orders

    public Task<CancelStandardOrderResult> CancelOrderAsync(string symbol, long orderId)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        if (_orders.TryGetValue(symbol, out var lookup) && lookup.TryGetValue(orderId, out var order))
        {
            _orders.GetOrCreate(symbol, () => new Dictionary<long, OrderQueryResult>())[orderId] = order = order with
            {
                Status = OrderStatus.Canceled
            };

            return Task.FromResult(new CancelStandardOrderResult(
                order.Symbol,
                order.ClientOrderId,
                order.OrderId,
                order.OrderListId,
                order.ClientOrderId,
                order.Price,
                order.OriginalQuantity,
                order.ExecutedQuantity,
                order.CummulativeQuoteQuantity,
                order.Status,
                order.TimeInForce,
                order.Type,
                order.Side));
        }

        throw new InvalidOperationException($"Order {symbol} {orderId} does not exist");
    }

    public Task<OrderResult> CreateOrderAsync(string symbol, OrderSide side, OrderType type, TimeInForce? timeInForce, decimal? quantity, decimal? quoteOrderQuantity, decimal? price, string? newClientOrderId, decimal? stopPrice, decimal? icebergQuantity)
    {
        var orderId = _orderIds.AddOrUpdate(symbol, () => 1, current => current + 1);

        var now = _clock.UtcNow;

        var order = new OrderQueryResult(
            symbol,
            orderId,
            0,
            newClientOrderId ?? Empty,
            price ?? 0,
            quantity ?? 0,
            0,
            quoteOrderQuantity ?? 0,
            OrderStatus.New,
            timeInForce ?? TimeInForce.GoodTillCanceled,
            type,
            side,
            stopPrice ?? 0,
            icebergQuantity ?? 0,
            now,
            now,
            true,
            quoteOrderQuantity ?? 0);

        _orders.GetOrCreate(symbol, () => new Dictionary<long, OrderQueryResult>())[orderId] = order;

        return Task.FromResult(new OrderResult(
            order.Symbol,
            order.OrderId,
            order.OrderListId,
            order.ClientOrderId,
            order.Time,
            order.Price,
            order.OriginalQuantity,
            order.ExecutedQuantity,
            order.CummulativeQuoteQuantity,
            order.Status,
            order.TimeInForce,
            order.Type,
            order.Side,
            ImmutableList<OrderFill>.Empty));
    }

    public Task<IReadOnlyCollection<OrderQueryResult>> GetAllOrdersAsync(string symbol, long? orderId, int? limit)
    {
        if (limit.HasValue && limit.Value > 1000) throw new ArgumentOutOfRangeException(nameof(limit));

        if (_orders.TryGetValue(symbol, out var lookup))
        {
            IEnumerable<OrderQueryResult> query = lookup.Values.OrderBy(x => x.OrderId);

            if (orderId.HasValue)
            {
                query = query.SkipWhile(x => x.OrderId < orderId.Value);
            }

            var result = query.Take(limit.GetValueOrDefault(100)).ToImmutableList();

            return Task.FromResult<IReadOnlyCollection<OrderQueryResult>>(result);
        }

        return Task.FromResult<IReadOnlyCollection<OrderQueryResult>>(ImmutableList<OrderQueryResult>.Empty);
    }

    public Task<IReadOnlyCollection<OrderQueryResult>> GetOpenOrdersAsync(string symbol)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        if (_orders.TryGetValue(symbol, out var lookup))
        {
            var result = lookup.Values.Where(x => x.Status.IsTransientStatus()).ToImmutableList();

            return Task.FromResult<IReadOnlyCollection<OrderQueryResult>>(result);
        }

        return Task.FromResult<IReadOnlyCollection<OrderQueryResult>>(ImmutableList<OrderQueryResult>.Empty);
    }

    public Task<OrderQueryResult> GetOrderAsync(string symbol, long? orderId, string? originalClientOrderId)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        if (orderId is null && originalClientOrderId is null) throw new ArgumentException($"Provide either '{nameof(orderId)}' or '{nameof(originalClientOrderId)}' parameters");
        if (orderId is not null && originalClientOrderId is not null) throw new ArgumentException($"Provide only one of '{nameof(orderId)}' or '{nameof(originalClientOrderId)}' parameters");

        if (_orders.TryGetValue(symbol, out var lookup))
        {
            if (orderId.HasValue && lookup.TryGetValue(orderId.Value, out var value))
            {
                return Task.FromResult(value);
            }
            else if (originalClientOrderId is not null)
            {
                var order = lookup.Values.FirstOrDefault(x => x.ClientOrderId == originalClientOrderId);
                if (order is not null)
                {
                    return Task.FromResult(order);
                }
            }
        }

        throw new InvalidOperationException("Order not found");
    }

    #endregion Orders

    #region Exchange

    private ExchangeInfo _info = ExchangeInfo.Empty;

    public Task<ExchangeInfo> GetExchangeInfoAsync()
    {
        return Task.FromResult(_info);
    }

    public Task SetExchangeInfoAsync(ExchangeInfo info)
    {
        if (info is null) throw new ArgumentNullException(nameof(info));

        _info = info;

        return Task.CompletedTask;
    }

    #endregion Exchange

    public Task<IReadOnlyCollection<SavingsBalance>> GetFlexibleProductPositionsAsync(string asset)
    {
        if (asset is null) throw new ArgumentNullException(nameof(asset));

        var result = _positions.TryGetValue(asset, out var items)
            ? items.Values.ToImmutableList()
            : ImmutableList<SavingsBalance>.Empty;

        return Task.FromResult<IReadOnlyCollection<SavingsBalance>>(result);
    }

    public Task SetFlexibleProductPositionsAsync(IEnumerable<SavingsBalance> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));

        foreach (var item in items)
        {
            _positions.GetOrCreate(item.Asset, () => new Dictionary<string, SavingsBalance>())[item.ProductId] = item;
        }

        return Task.CompletedTask;
    }

    public Task<SavingsQuota?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(
        string productId,
        SavingsRedemptionType type)
    {
        if (productId is null) throw new ArgumentNullException(nameof(productId));

        var result = _quotas.TryGetValue((productId, type), out var value) ? value : null;

        return Task.FromResult(result);
    }

    public Task SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, SavingsRedemptionType type, SavingsQuota item)
    {
        if (productId is null) throw new ArgumentNullException(nameof(productId));
        if (item is null) throw new ArgumentNullException(nameof(item));

        _quotas[(productId, type)] = item;

        return Task.CompletedTask;
    }

    public Task Set24hTickerPriceChangeStatisticsAsync(Ticker ticker)
    {
        if (ticker is null) throw new ArgumentNullException(nameof(ticker));

        _tickers[ticker.Symbol] = ticker;

        return Task.CompletedTask;
    }

    public Task<Ticker> Get24hTickerPriceChangeStatisticsAsync(string symbol)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        if (_tickers.TryGetValue(symbol, out var ticker))
        {
            return Task.FromResult(ticker);
        }

        throw new KeyNotFoundException(symbol);
    }

    public Task<IReadOnlyCollection<Ticker>> Get24hTickerPriceChangeStatisticsAsync()
    {
        return Task.FromResult<IReadOnlyCollection<Ticker>>(_tickers.Values);
    }

    public Task<AccountInfo> GetAccountInfoAsync()
    {
        return Task.FromResult(_account);
    }

    public Task SetAccountInfoAsync(AccountInfo info)
    {
        if (info is null) throw new ArgumentNullException(nameof(info));

        _account = info;

        return Task.CompletedTask;
    }

    public Task SetAccountTradeAsync(AccountTrade trade)
    {
        if (trade is null) throw new ArgumentNullException(nameof(trade));

        _trades.GetOrCreate(trade.Symbol, () => new())[trade.Id] = trade;

        return Task.CompletedTask;
    }

    public Task<ImmutableSortedSet<AccountTrade>> GetAccountTradesAsync(string symbol, long? fromId, int? limit)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        if (!_trades.TryGetValue(symbol, out var lookup))
        {
            return Task.FromResult(ImmutableSortedSet<AccountTrade>.Empty);
        }

        var query = lookup.Values.AsEnumerable();

        if (fromId.HasValue)
        {
            query = query.Where(x => x.Id >= fromId.Value);
        }

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        var result = query.ToImmutableSortedSet(AccountTrade.KeyComparer);

        return Task.FromResult(result);
    }
}