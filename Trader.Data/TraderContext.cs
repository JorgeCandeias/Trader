using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Trader.Data
{
    internal class TraderContext : DbContext
    {
        public TraderContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<OrderEntity> Orders { get; set; } = null!;
        public DbSet<TradeEntity> Trades { get; set; } = null!;
        public DbSet<OrderGroupEntity> OrderGroups { get; set; } = null!;
        public DbSet<OrderGroupDetailEntity> OrderGroupDetails { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }

    internal record OrderEntity(
        string Symbol,
        long OrderId,
        long OrderListId,
        string ClientOrderId,
        decimal Price,
        decimal OriginalQuantity,
        decimal ExecutedQuantity,
        decimal CummulativeQuoteQuantity,
        int Status,
        int TimeInForce,
        int Type,
        int Side,
        decimal StopPrice,
        decimal IcebergQuantity,
        DateTime Time,
        DateTime UpdateTime,
        bool IsWorking,
        decimal OriginalQuoteOrderQuantity);

    internal record TradeEntity(
        string Symbol,
        long Id,
        long OrderId,
        long OrderListId,
        decimal Price,
        decimal Quantity,
        decimal QuoteQuantity,
        decimal Commission,
        string CommissionAsset,
        DateTime Time,
        bool IsBuyer,
        bool IsMaker,
        bool IsBestMatch);

    internal record OrderGroupEntity
    {
        public long Id { get; set; }
        public DateTime CreatedTime { get; set; }

        public List<OrderGroupDetailEntity> Details { get; set; } = null!;
    }

    internal class OrderGroupDetailEntity
    {
        public long Id { get; set; }
        public long GroupId { get; set; }
        public long OrderId { get; set; }
        public DateTime CreatedTime { get; set; }

        public OrderGroupEntity Group { get; set; } = null!;
        public OrderEntity Order { get; set; } = null!;
    }
}