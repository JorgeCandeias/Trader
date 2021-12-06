using AutoMapper;

namespace Outcompute.Trader.Models.Collections
{
    internal class ImmutableSortedOrderSetConverter<T> : ITypeConverter<IEnumerable<T>, ImmutableSortedOrderSet>
    {
        public ImmutableSortedOrderSet Convert(IEnumerable<T> source, ImmutableSortedOrderSet destination, ResolutionContext context)
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsNotNull(context, nameof(context));

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