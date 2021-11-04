using AutoMapper;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models.Collections
{
    internal class ImmutableDictionaryConverter<TSourceKey, TSourceValue, TDestinationKey, TDestinationValue> : ITypeConverter<IEnumerable<KeyValuePair<TSourceKey, TSourceValue>>, ImmutableDictionary<TDestinationKey, TDestinationValue>>
        where TSourceKey : notnull
        where TDestinationKey : notnull
    {
        public ImmutableDictionary<TDestinationKey, TDestinationValue> Convert(IEnumerable<KeyValuePair<TSourceKey, TSourceValue>> source, ImmutableDictionary<TDestinationKey, TDestinationValue> destination, ResolutionContext context)
        {
            // return empty list when null to follow dotnet convention
            if (source is null) return ImmutableDictionary<TDestinationKey, TDestinationValue>.Empty;

            // return source as-is if already an immutable list of the destination type
            if (source is ImmutableDictionary<TDestinationKey, TDestinationValue> immutable) return immutable;

            // convert to immutable list of the destination otherwise
            var builder = ImmutableDictionary.CreateBuilder<TDestinationKey, TDestinationValue>();
            foreach (var item in source)
            {
                var key = context.Mapper.Map<TDestinationKey>(item.Key);
                var value = context.Mapper.Map<TDestinationValue>(item.Value);

                builder.Add(key, value);
            }
            return builder.ToImmutable();
        }
    }
}