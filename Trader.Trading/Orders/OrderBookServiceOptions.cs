using System.ComponentModel.DataAnnotations;
using static System.String;

namespace Trader.Trading.Orders
{
    public class OrderBookServiceOptions
    {
        [Required]
        public string Symbol { get; set; } = Empty;
    }
}