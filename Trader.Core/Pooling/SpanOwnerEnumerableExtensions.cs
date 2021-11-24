using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace System.Collections.Generic;

public static class SpanOwnerEnumerableExtensions
{
    public static SpanOwner<T> ToSpanOwner<T>(this IEnumerable<T> source, out int count)
    {
        Guard.IsNotNull(source, nameof(source));

        if (!source.TryGetNonEnumeratedCount(out var size))
        {
            size = 1024;
        }

        var owner = SpanOwner<T>.Allocate(size);
        var span = owner.Span;
        count = 0;

        foreach (var item in source)
        {
            if (++count > owner.Length)
            {
                var newOwner = SpanOwner<T>.Allocate(owner.Length * 2);
                owner.Span.CopyTo(newOwner.Span);
                owner.Dispose();
                owner = newOwner;
                span = newOwner.Span;
            }

            span[count] = item;
        }

        return owner;
    }
}