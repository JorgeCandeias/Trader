using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;

namespace Trader.Data
{
    internal class SqliteTraderRepository : ITraderRepository, IHostedService
    {
        public readonly IDbContextFactory<TraderContext> _factory;
        public readonly IMapper _mapper;
        public readonly ISystemClock _clock;

        public SqliteTraderRepository(IDbContextFactory<TraderContext> factory, IMapper mapper, ISystemClock clock)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));

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

        public async Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            var entity = _mapper.Map<OrderEntity>(order);

            var exists = await context.Orders.AnyAsync(x => x.OrderId == entity.OrderId, cancellationToken);
            if (exists)
            {
                context.Update(entity);
            }
            else
            {
                context.Add(entity);
            }
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            foreach (var order in orders)
            {
                var entity = _mapper.Map<OrderEntity>(order);
                var exists = await context.Orders.AnyAsync(x => x.OrderId == entity.OrderId, cancellationToken);
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

        #region Order Groups

        public async Task<OrderGroup> CreateOrderGroupAsync(IEnumerable<long> orderIds, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            // create the new group
            var group = new OrderGroupEntity
            {
                CreatedTime = _clock.UtcNow
            };
            var entity = context.OrderGroups.Add(group);
            await context.SaveChangesAsync(cancellationToken);

            // add details to the group
            foreach (var orderId in orderIds)
            {
                context.OrderGroupDetails.Add(new OrderGroupDetailEntity
                {
                    GroupId = group.Id,
                    OrderId = orderId,
                    CreatedTime = _clock.UtcNow
                });
            }
            await context.SaveChangesAsync(cancellationToken);

            // query the order group again with orders
            var result = await context
                .OrderGroups
                .Include(x => x.Details)
                .ThenInclude(x => x.Order)
                .Where(x => x.Id == group.Id)
                .SingleAsync(cancellationToken);

            return _mapper.Map<OrderGroup>(result);
        }

        public async Task<OrderGroup?> GetOrderGroupAsync(long id, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            var result = await context
                .OrderGroups
                .Include(x => x.Details)
                .ThenInclude(x => x.Order)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

            return _mapper.Map<OrderGroup>(result);
        }

        public async Task<OrderGroup> GetLatestOrCreateOrderGroupForOrdersAsync(IEnumerable<long> orderIds, CancellationToken cancellationToken = default)
        {
            var result = await GetLatestOrderGroupForOrdersAsync(orderIds, cancellationToken);
            if (result is null)
            {
                result = await CreateOrderGroupAsync(orderIds, cancellationToken);
            }
            return result;
        }

        public Task<OrderGroup> GetLatestOrCreateOrderGroupForOrderAsync(long orderId, CancellationToken cancellationToken = default)
        {
            return GetLatestOrCreateOrderGroupForOrdersAsync(Enumerable.Repeat(orderId, 1), cancellationToken);
        }

        public Task<OrderGroup?> GetLatestOrderGroupForOrdersAsync(IEnumerable<long> orderIds, CancellationToken cancellationToken = default)
        {
            if (orderIds is null) throw new ArgumentNullException(nameof(orderIds));
            if (!orderIds.Any()) throw new ArgumentOutOfRangeException(nameof(orderIds));

            return InnerGetLatestOrderGroupForOrdersAsync(orderIds, cancellationToken);
        }

        private async Task<OrderGroup?> InnerGetLatestOrderGroupForOrdersAsync(IEnumerable<long> orderIds, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            // attempt to identify a unique group to which all specified orders
            OrderGroupEntity? elected = null;
            foreach (var orderId in orderIds)
            {
                var detail = await context
                    .OrderGroupDetails
                    .Include(x => x.Group)
                    .Where(x => x.OrderId == orderId)
                    .OrderByDescending(x => x.GroupId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (detail is null)
                {
                    // give up if there is no group for this order yet
                    return null;
                }
                else if (elected is null)
                {
                    // keep the first group
                    elected = detail.Group;
                }
                else if (detail.Group.Id != elected.Id)
                {
                    // give up on different group found
                    return null;
                }
            }

            // give up on no group found
            if (elected is null) return null;

            // load all the details for the surviving group
            await context.Entry(elected).Collection(x => x.Details).LoadAsync(cancellationToken);

            // load all the orders for the loaded details
            foreach (var detail in elected.Details)
            {
                await context.Entry(detail).Reference(x => x.Order).LoadAsync(cancellationToken);
            }

            return _mapper.Map<OrderGroup>(elected);
        }

        public Task<OrderGroup?> GetLatestOrderGroupForOrderAsync(long orderId, CancellationToken cancellationToken = default)
        {
            return GetLatestOrderGroupForOrdersAsync(Enumerable.Repeat(orderId, 1), cancellationToken);
        }

        #endregion Order Groups

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

        public async Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            var entity = _mapper.Map<TradeEntity>(trade);

            var exists = await context.Trades.AnyAsync(x => x.Id == entity.Id, cancellationToken);
            if (exists)
            {
                context.Update(entity);
            }
            else
            {
                context.Add(entity);
            }
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            foreach (var trade in trades)
            {
                var entity = _mapper.Map<TradeEntity>(trade);
                var exists = await context.Trades.AnyAsync(x => x.Id == entity.Id, cancellationToken);
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

        #endregion IHostedService
    }
}