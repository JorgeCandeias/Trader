using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Trader.Data
{
    public class SqliteTraderRepositoryOptions
    {
        [Required]
        public string ConnectionString { get; set; } = Empty;
    }
}