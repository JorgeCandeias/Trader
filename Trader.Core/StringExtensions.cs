namespace System
{
    public static class TraderStringExtensions
    {
        /// <inheritdoc cref="string.IsNullOrWhiteSpace(string?)"/>
        public static bool IsNullOrWhiteSpace(this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }
    }
}