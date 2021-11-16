namespace System;

public static class DecimalExtensions
{
    public static decimal SafeDivideBy(this decimal numerator, decimal denominator) => denominator is 0 ? 0 : numerator / denominator;
}