using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Trader.Data.Sql
{
    public class SqlTraderRepositoryOptions
    {
        [Required]
        public string ConnectionString { get; set; } = Empty;
    }
}