using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outcompute.Trader.Models
{
    public record LeftDailyRedemptionQuotaOnFlexibleProduct(
        string Asset,
        decimal DailyQuota,
        decimal LeftQuota,
        decimal MinRedemptionAmount);
}
