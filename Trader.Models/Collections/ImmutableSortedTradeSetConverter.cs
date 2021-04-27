using AutoMapper;
using System;
using System.Collections.Generic;

namespace Trader.Models.Collections
{
    internal class ImmutableSortedTradeSetConverter<T> : ITypeConverter<IEnumerable<T>, ImmutableSortedTradeSet>
    {
        public ImmutableSortedTradeSet Convert(IEnumerable<T> source, ImmutableSortedTradeSet destination, ResolutionContext context)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (destination is not null) throw new ArgumentOutOfRangeException(nameof(destination));
            if (context is null) throw new ArgumentNullException(nameof(context));

            var builder = ImmutableSortedTradeSet.CreateBuilder();

            foreach (var item in source)
            {
                var mapped = context.Mapper.Map<AccountTrade>(item);

                builder.Add(mapped);
            }

            return builder.ToImmutable();
        }
    }
}