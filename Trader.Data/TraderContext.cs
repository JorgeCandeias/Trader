using Microsoft.EntityFrameworkCore;
using System;

namespace Trader.Data
{
    internal class TraderContext : DbContext
    {
        public TraderContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<OrderEntity> Orders { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<OrderEntity>().HasKey(x => x.OrderId);
            modelBuilder.Entity<OrderEntity>().HasIndex(x => new { x.Symbol, x.OrderId });

            modelBuilder.Entity<TradeEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<TradeEntity>().HasIndex(x => new { x.Symbol, x.Id });
            modelBuilder.Entity<TradeEntity>().HasIndex(x => new { x.Symbol, x.OrderId });
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
}