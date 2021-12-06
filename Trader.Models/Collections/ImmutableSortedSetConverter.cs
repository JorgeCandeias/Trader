using AutoMapper;

namespace Outcompute.Trader.Models.Collections;

internal class ImmutableSortedSetConverter<TSource, TDestination> : ITypeConverter<IEnumerable<TSource>, ImmutableSortedSet<TDestination>>
{
    public ImmutableSortedSet<TDestination> Convert(IEnumerable<TSource> source, ImmutableSortedSet<TDestination> destination, ResolutionContext context)
    {
        // attempt to identify the user defined comparer
        IComparer<TDestination>? comparer = null;
        if (context.Items.TryGetValue(nameof(ImmutableSortedSet<TDestination>.KeyComparer), out var contextComparer))
        {
            if (contextComparer is not IComparer<TDestination> typed)
            {
                throw new InvalidOperationException($"Context property '{nameof(ImmutableSortedSet<TDestination>.KeyComparer)}' must be of type '{nameof(IComparer<TDestination>)}'");
            }
            comparer = typed;
        }

        // return empty list when null to follow dotnet convention
        if (source is null)
        {
            return ImmutableSortedSet<TDestination>.Empty.WithComparer(comparer);
        }

        // attempt to return source as-is if already an immutable list of the destination type
        if (source is ImmutableSortedSet<TDestination> immutable)
        {
            return immutable.WithComparer(comparer);
        }

        // convert to immutable of the destination otherwise
        var builder = ImmutableSortedSet.CreateBuilder(comparer);
        foreach (var item in source)
        {
            builder.Add(context.Mapper.Map<TDestination>(item));
        }
        return builder.ToImmutable();
    }
}