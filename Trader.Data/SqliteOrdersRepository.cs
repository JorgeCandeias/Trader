using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Data
{
    internal class SqliteOrdersRepository : IOrdersRepository, IHostedService
    {
        public readonly IDbContextFactory<TraderContext> _factory;
        public readonly IMapper _mapper;

        public SqliteOrdersRepository(IDbContextFactory<TraderContext> factory, IMapper mapper)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task AddOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();

            var entity = _mapper.Map<OrderEntity>(order);
            context.Orders.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var context = _factory.CreateDbContext();

            await context.Database.MigrateAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}