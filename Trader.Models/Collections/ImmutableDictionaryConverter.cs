using AutoMapper;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models.Collections
{
    internal class ImmutableDictionaryConverter<TKey, TValue> : ITypeConverter<IEnumerable<KeyValuePair<TKey, TValue>>, ImmutableDictionary<TKey, TValue>>
        where TKey : notnull
    {
        public ImmutableDictionary<TKey, TValue> Convert(IEnumerable<KeyValuePair<TKey, TValue>> source, ImmutableDictionary<TKey, TValue> destination, ResolutionContext context)
        {
            // return empty list when null to follow dotnet convention
            if (source is null) return ImmutableDictionary<TKey, TValue>.Empty;

            // return source as-is if already an immutable list of the destination type
            if (source is ImmutableDictionary<TKey, TValue> immutable) return immutable;

            // convert to immutable list of the destination otherwise
            var builder = ImmutableDictionary.CreateBuilder<TKey, TValue>();
            foreach (var item in source)
            {
                builder.Add(item);
            }
            return builder.ToImmutable();
        }
    }
}