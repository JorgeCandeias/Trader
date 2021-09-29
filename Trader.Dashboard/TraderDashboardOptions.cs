using System.ComponentModel.DataAnnotations;

namespace Outcompute.Trader.Dashboard
{
    public class TraderDashboardOptions
    {
        [Required]
        public string Host { get; set; } = "*";

        public int Port { get; set; } = 8081;

        public bool UseHttps { get; set; }
    }
}