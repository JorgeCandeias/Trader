namespace Outcompute.Trader.Models;

public static class KlineInternalDateTimeExtensions
{
    public static IEnumerable<DateTime> Range(this KlineInterval interval, DateTime start, DateTime end)
    {
        start = start.AdjustToNext(interval);
        end = end.AdjustToPrevious(interval);

        for (var current = start; current <= end; current = current.Add(interval))
        {
            yield return current;
        }
    }

    public static IEnumerable<DateTime> RangeDescending(this KlineInterval interval, DateTime start, DateTime end)
    {
        start = start.AdjustToNext(interval);
        end = end.AdjustToPrevious(interval);

        for (var current = end; current >= start; current = current.Subtract(interval))
        {
            yield return current;
        }
    }

    public static DateTime AdjustToPrevious(this DateTime value, KlineInterval interval)
    {
        return interval switch
        {
            KlineInterval.Minutes1 => new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0),
            KlineInterval.Minutes3 => new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute - (value.Minute % 3), 0),
            KlineInterval.Minutes5 => new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute - (value.Minute % 5), 0),
            KlineInterval.Minutes15 => new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute - (value.Minute % 15), 0),
            KlineInterval.Minutes30 => new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute - (value.Minute % 30), 0),
            KlineInterval.Hours1 => new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0),
            KlineInterval.Hours2 => new DateTime(value.Year, value.Month, value.Day, value.Hour - (value.Hour % 2), 0, 0),
            KlineInterval.Hours4 => new DateTime(value.Year, value.Month, value.Day, value.Hour - (value.Hour % 4), 0, 0),
            KlineInterval.Hours6 => new DateTime(value.Year, value.Month, value.Day, value.Hour - (value.Hour % 6), 0, 0),
            KlineInterval.Hours8 => new DateTime(value.Year, value.Month, value.Day, value.Hour - (value.Hour % 8), 0, 0),
            KlineInterval.Hours12 => new DateTime(value.Year, value.Month, value.Day, value.Hour - (value.Hour % 12), 0, 0),
            KlineInterval.Days1 => new DateTime(value.Year, value.Month, value.Day),
            KlineInterval.Days3 => new DateTime(value.Year, value.Month, value.Day - ((value.Day - 1) % 3)),
            KlineInterval.Weeks1 => value.Subtract(TimeSpan.FromDays((int)value.DayOfWeek)).Date,
            KlineInterval.Months1 => new DateTime(value.Year, value.Month, 1),

            _ => throw new ArgumentOutOfRangeException(nameof(interval))
        };
    }

    public static DateTime AdjustToNext(this DateTime value, KlineInterval interval)
    {
        var previous = value.AdjustToPrevious(interval);

        return value == previous ? value : previous.Add(interval);
    }

    public static DateTime Add(this DateTime value, KlineInterval interval, int count = 1)
    {
        return interval switch
        {
            KlineInterval.Minutes1 => value.AddMinutes(1 * count),
            KlineInterval.Minutes3 => value.AddMinutes(3 * count),
            KlineInterval.Minutes5 => value.AddMinutes(5 * count),
            KlineInterval.Minutes15 => value.AddMinutes(15 * count),
            KlineInterval.Minutes30 => value.AddMinutes(30 * count),
            KlineInterval.Hours1 => value.AddHours(1 * count),
            KlineInterval.Hours2 => value.AddHours(2 * count),
            KlineInterval.Hours4 => value.AddHours(4 * count),
            KlineInterval.Hours6 => value.AddHours(6 * count),
            KlineInterval.Hours8 => value.AddHours(8 * count),
            KlineInterval.Hours12 => value.AddHours(12 * count),
            KlineInterval.Days1 => value.AddDays(1 * count),
            KlineInterval.Days3 => value.AddDays(3 * count),
            KlineInterval.Weeks1 => value.AddDays(7 * count),
            KlineInterval.Months1 => value.AddMonths(1 * count),

            _ => throw new ArgumentOutOfRangeException(nameof(interval))
        };
    }

    public static DateTime Subtract(this DateTime value, KlineInterval interval, int count = 1)
    {
        return value.Add(interval, count * -1);
    }
}