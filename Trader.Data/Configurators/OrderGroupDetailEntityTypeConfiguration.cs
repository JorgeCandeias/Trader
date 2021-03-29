using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trader.Data.Configurators
{
    internal class OrderGroupDetailEntityTypeConfiguration : IEntityTypeConfiguration<OrderGroupDetailEntity>
    {
        public void Configure(EntityTypeBuilder<OrderGroupDetailEntity> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new { x.GroupId, x.OrderId });
            builder.HasOne(x => x.Group).WithMany(x => x.Details).HasForeignKey(x => x.GroupId);
            builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId);
        }
    }
}