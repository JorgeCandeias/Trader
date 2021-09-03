using FastMember;

namespace Outcompute.Trader.Data.Sql
{
    internal static class TypeAccessorCache<T>
    {
        public static TypeAccessor TypeAccessor { get; } = TypeAccessor.Create(typeof(T));
    }
}