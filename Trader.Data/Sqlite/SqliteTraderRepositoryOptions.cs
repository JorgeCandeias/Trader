using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Trader.Data.Sqlite
{
    public class SqliteTraderRepositoryOptions
    {
        [Required]
        public string ConnectionString { get; set; } = Empty;
    }
}