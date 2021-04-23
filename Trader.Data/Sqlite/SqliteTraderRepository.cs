using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Data.Sqlite
{
    [Obsolete("To remove")]
    internal class SqliteTraderRepository : ITraderRepository, IHostedService
    {
        public readonly IDbContextFactory<TraderContext> _factory;
        public readonly IMapper _mapper;

        public SqliteTraderRepository(IDbContextFactory<TraderContext> factory, IMapper mapper)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            _transientStatuses = Enum.GetValues<OrderStatus>().Where(x => x.IsTransientStatus()).Select(x => _mapper.Map<int>(x)).ToArray();
        }

        #region Helpers

        private readonly int[] _transientStatuses;

        #endregion Helpers

        #region Orders

        public async Task<long> GetMinTransientOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            return await context.Orders
                .Where(x => x.Symbol == symbol)
                .Where(x => _transientStatuses.Contains(x.Status))
                .Select(x => x.OrderId)
                .DefaultIfEmpty()
                .MinAsync(cancellationToken);
        }

        public async Task<long> GetMaxOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            return await context.Orders
                .Where(x => x.Symbol == symbol)
                .Select(x => x.OrderId)
                .DefaultIfEmpty()
                .MaxAsync(cancellationToken);
        }

        public async Task<SortedOrderSet> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            var entities = await context.Orders
                .Where(x => x.Symbol == symbol)
                .ToListAsync(cancellationToken);

            return _mapper.Map<SortedOrderSet>(entities);
        }

        public async Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            foreach (var order in orders)
            {
                var entity = _mapper.Map<OrderEntity>(order);
                var exists = await context.Orders.AnyAsync(x => x.Symbol == entity.Symbol && x.OrderId == entity.OrderId, cancellationToken);
                if (exists)
                {
                    context.Orders.Update(entity);
                }
                else
                {
                    context.Orders.Add(entity);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<SortedOrderSet> GetTransientOrdersAsync(string symbol, OrderSide? orderSide = default, bool? significant = default, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            var query = context.Orders
                .Where(x => x.Symbol == symbol)
                .Where(x => _transientStatuses.Contains(x.Status));

            if (orderSide.HasValue)
            {
                var orderSideId = _mapper.Map<int>(orderSide.Value);
                query = query.Where(x => x.Side == orderSideId);
            }

            if (significant.HasValue)
            {
                if (significant.Value)
                {
                    query = query.Where(x => x.ExecutedQuantity > 0m);
                }
                else
                {
                    query = query.Where(x => x.ExecutedQuantity == 0m);
                }
            }

            var result = await query.ToListAsync(cancellationToken);

            return _mapper.Map<SortedOrderSet>(result);
        }

        #endregion Orders

        #region Trades

        public async Task<long> GetMaxTradeIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            return await context.Trades
                .Where(x => x.Symbol == symbol)
                .Select(x => x.Id)
                .DefaultIfEmpty()
                .MaxAsync(cancellationToken);
        }

        public async Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            foreach (var trade in trades)
            {
                var entity = _mapper.Map<TradeEntity>(trade);
                var exists = await context.Trades.AnyAsync(x => x.Symbol == entity.Symbol && x.Id == entity.Id, cancellationToken);
                if (exists)
                {
                    context.Update(entity);
                }
                else
                {
                    context.Add(entity);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<SortedTradeSet> GetTradesAsync(string symbol, long? orderId = default, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            var query = context.Trades
                .Where(x => x.Symbol == symbol);

            if (orderId.HasValue)
            {
                query = query.Where(x => x.OrderId == orderId.Value);
            }

            var result = await query.ToListAsync(cancellationToken);

            return _mapper.Map<SortedTradeSet>(result);
        }

        #endregion Trades

        #region IHostedService

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var context = _factory.CreateDbContext();

            await context.Database.MigrateAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ApplyAsync(CancelStandardOrderResult result, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task ApplyAsync(OrderResult result, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        #endregion IHostedService
    }
}