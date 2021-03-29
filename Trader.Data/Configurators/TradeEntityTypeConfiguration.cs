using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trader.Data.Configurators
{
    internal class TradeEntityTypeConfiguration : IEntityTypeConfiguration<TradeEntity>
    {
        public void Configure(EntityTypeBuilder<TradeEntity> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.Symbol, x.Id });
            builder.HasIndex(x => new { x.Symbol, x.OrderId });
        }
    }
}