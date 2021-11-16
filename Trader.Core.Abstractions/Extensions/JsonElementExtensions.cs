using System.Globalization;
using static System.String;

namespace System.Text.Json;

public static class JsonElementExtensions
{
    public static string GetRequiredString(this JsonElement element)
    {
        var value = element.GetString();

        if (IsNullOrWhiteSpace(value)) throw new JsonException("Value is null or empty");

        return value;
    }

    public static decimal GetRequiredDecimalFromString(this JsonElement element)
    {
        return decimal.Parse(element.GetRequiredString(), CultureInfo.InvariantCulture);
    }
}