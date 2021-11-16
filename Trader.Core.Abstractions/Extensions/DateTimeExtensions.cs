namespace System;

public static class DateTimeExtensions
{
    public static DateTime Previous(this DateTime value, DayOfWeek dayOfWeek, int count = 1)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));

        // jump back all excess weeks
        if (count > 1)
        {
            value = value.AddDays(-(count - 1) * 7);
        }

        // jump back the remaining days if necessary
        if (value.DayOfWeek < dayOfWeek)
        {
            var jump = 7 - ((int)dayOfWeek - (int)value.DayOfWeek);
            value = value.AddDays(-jump);
        }
        else if (value.DayOfWeek > dayOfWeek)
        {
            var jump = (int)value.DayOfWeek - (int)dayOfWeek;
            value = value.AddDays(-jump);
        }

        return value;
    }
}