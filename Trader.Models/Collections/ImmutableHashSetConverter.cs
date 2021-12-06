using AutoMapper;

namespace Outcompute.Trader.Models.Collections;

internal class ImmutableHashSetConverter<TSource, TDestination> : ITypeConverter<IEnumerable<TSource>, ImmutableHashSet<TDestination>>
{
    public ImmutableHashSet<TDestination> Convert(IEnumerable<TSource> source, ImmutableHashSet<TDestination> destination, ResolutionContext context)
    {
        // return empty list when null to follow dotnet convention
        if (source is null)
        {
            return ImmutableHashSet<TDestination>.Empty;
        }

        // return source as-is if already an immutable list of the destination type
        if (source is ImmutableHashSet<TDestination> immutable)
        {
            return immutable;
        }

        // convert to immutable list of the destination otherwise
        var builder = ImmutableHashSet.CreateBuilder<TDestination>();
        foreach (var item in source)
        {
            builder.Add(context.Mapper.Map<TDestination>(item));
        }
        return builder.ToImmutable();
    }
}