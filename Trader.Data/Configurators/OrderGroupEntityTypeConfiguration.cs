using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trader.Data.Configurators
{
    internal class OrderGroupEntityTypeConfiguration : IEntityTypeConfiguration<OrderGroupEntity>
    {
        public void Configure(EntityTypeBuilder<OrderGroupEntity> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }
}