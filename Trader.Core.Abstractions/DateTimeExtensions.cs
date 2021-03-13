using System;

namespace Trader.Core
{
    public static class DateTimeExtensions
    {
        public static DateTime Previous(this DateTime value, DayOfWeek dayOfWeek)
        {
            while (value.DayOfWeek != dayOfWeek)
            {
                value = value.AddDays(-1);
            }

            return value;
        }
    }
}