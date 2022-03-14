namespace Outcompute.Trader.Core.Mathematics
{
    public enum MinMaxBehavior
    {
        NullWins,
        NonNullWins
    }

    /// <summary>
    /// Math functions for nullable types.
    /// </summary>
    public static class MathN
    {
        /// <inheritdoc cref="Math.Max(decimal, decimal)"/>
        public static decimal? Max(decimal? val1, decimal? val2, MinMaxBehavior behavior = MinMaxBehavior.NullWins)
        {
            switch (behavior)
            {
                case MinMaxBehavior.NullWins:
                    if (val1.HasValue && val2.HasValue)
                    {
                        return Math.Max(val1.Value, val2.Value);
                    }
                    else
                    {
                        return null;
                    }

                case MinMaxBehavior.NonNullWins:
                    if (val1.HasValue)
                    {
                        if (val2.HasValue)
                        {
                            return Math.Max(val1.Value, val2.Value);
                        }
                        else
                        {
                            return val1;
                        }
                    }
                    else
                    {
                        return val2;
                    }
            }

            return null;
        }

        /// <inheritdoc cref="Math.Min(decimal, decimal)"/>
        public static decimal? Min(decimal? val1, decimal? val2, MinMaxBehavior behavior = MinMaxBehavior.NullWins)
        {
            switch (behavior)
            {
                case MinMaxBehavior.NullWins:
                    if (val1.HasValue && val2.HasValue)
                    {
                        return Math.Min(val1.Value, val2.Value);
                    }
                    else
                    {
                        return null;
                    }

                case MinMaxBehavior.NonNullWins:
                    if (val1.HasValue)
                    {
                        if (val2.HasValue)
                        {
                            return Math.Min(val1.Value, val2.Value);
                        }
                        else
                        {
                            return val1;
                        }
                    }
                    else
                    {
                        return val2;
                    }
            }

            return null;
        }

        public static decimal? SafeDiv(decimal? val1, decimal? val2)
        {
            if (val1.HasValue && val2.HasValue && val2.Value != 0)
            {
                return val1.Value / val2.Value;
            }

            return null;
        }
    }
}