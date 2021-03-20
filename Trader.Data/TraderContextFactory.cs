using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Trader.Data
{
    internal class TraderContextFactory : IDesignTimeDbContextFactory<TraderContext>
    {
        public TraderContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<TraderContext>();

            builder.UseSqlite("Data Source=:memory:");

            return new TraderContext(builder.Options);
        }
    }
}