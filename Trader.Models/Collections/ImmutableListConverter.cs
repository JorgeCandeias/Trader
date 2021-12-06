using AutoMapper;

namespace Outcompute.Trader.Models.Collections;

internal class ImmutableListConverter<TSource, TDestination> : ITypeConverter<IEnumerable<TSource>, ImmutableList<TDestination>>
{
    public ImmutableList<TDestination> Convert(IEnumerable<TSource> source, ImmutableList<TDestination> destination, ResolutionContext context)
    {
        // return empty list when null to follow dotnet convention
        if (source is null)
        {
            return ImmutableList<TDestination>.Empty;
        }

        // return source as-is if already an immutable list of the destination type
        if (source is ImmutableList<TDestination> immutable)
        {
            return immutable;
        }

        // convert to immutable list of the destination otherwise
        var builder = ImmutableList.CreateBuilder<TDestination>();
        foreach (var item in source)
        {
            builder.Add(context.Mapper.Map<TDestination>(item));
        }
        return builder.ToImmutable();
    }
}