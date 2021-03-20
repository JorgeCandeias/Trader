using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Trader.Data
{
    public class SqliteRepositoryOptions
    {
        [Required]
        public string ConnectionString { get; set; } = Empty;
    }
}