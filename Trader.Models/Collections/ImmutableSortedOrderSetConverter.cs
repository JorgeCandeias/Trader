using AutoMapper;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Models.Collections
{
    internal class ImmutableSortedOrderSetConverter<T> : ITypeConverter<IEnumerable<T>, ImmutableSortedOrderSet>
    {
        public ImmutableSortedOrderSet Convert(IEnumerable<T> source, ImmutableSortedOrderSet destination, ResolutionContext context)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (destination is not null) throw new ArgumentOutOfRangeException(nameof(destination));
            if (context is null) throw new ArgumentNullException(nameof(context));

            var builder = ImmutableSortedOrderSet.CreateBuilder();

            foreach (var item in source)
            {
                var mapped = context.Mapper.Map<OrderQueryResult>(item);

                builder.Add(mapped);
            }

            return builder.ToImmutable();
        }
    }
}