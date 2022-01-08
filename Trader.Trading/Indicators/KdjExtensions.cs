namespace System.Collections.Generic;

public static class KdjExtensions
{
    public enum KdjSide
    {
        None,
        Up,
        Down
    }

    public record struct KdjValue
    {
        public decimal K { get; init; }
        public decimal D { get; init; }
        public decimal J { get; init; }

        public KdjSide Side
        {
            get
            {
                if (K > D && J > K) return KdjSide.Up;
                if (K < D && J < K) return KdjSide.Down;
                return KdjSide.None;
            }
        }

        public static KdjValue Empty => new();
    }

    public static IEnumerable<KdjValue> Kdj(this IEnumerable<Kline> source, int periods = 9, int ma1 = 3, int ma2 = 3)
    {
        Guard.IsNotNull(source, nameof(source));

        var items = source.GetEnumerator();
        var mins = source.Select(x => x.LowPrice).MovingMin(periods).GetEnumerator();
        var maxes = source.Select(x => x.HighPrice).MovingMax(periods).GetEnumerator();

        // keep track of the previous value
        KdjValue prev;

        // calculate the first output
        if (items.MoveNext() && mins.MoveNext() && maxes.MoveNext())
        {
            var item = items.Current;
            var min = mins.Current;
            var max = maxes.Current;

            var rsv = (item.ClosePrice - min) / (max - min) * 100M;
            var a = rsv;
            var b = a;
            var e = (3M * a) - (2M * b);

            prev = new KdjValue
            {
                K = a,
                D = b,
                J = e
            };

            yield return prev;

            // calculate the remaining outputs
            while (items.MoveNext() && mins.MoveNext() && maxes.MoveNext())
            {
                item = items.Current;
                min = mins.Current;
                max = maxes.Current;

                rsv = (item.ClosePrice - min) / (max - min) * 100M;
                a = (rsv + (ma1 - 1) * prev.K) / ma1;
                b = (a + (ma2 - 1) * prev.D) / ma2;
                e = (3 * a) - (2 * b);

                prev = new KdjValue
                {
                    K = a,
                    D = b,
                    J = e
                };

                yield return prev;
            }
        }
    }

    public static IEnumerable<decimal> MovingMin(this IEnumerable<decimal> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));

        var queue = new Queue<decimal>();
        var counts = new Dictionary<decimal, int>();
        var set = new SortedSet<decimal>();

        foreach (var item in source)
        {
            // remove the oldest item
            if (queue.Count >= periods)
            {
                var old = queue.Dequeue();
                if (--counts[old] == 0)
                {
                    set.Remove(old);
                }
            }

            // track the new item
            queue.Enqueue(item);

            if (counts.TryGetValue(item, out var count))
            {
                counts[item] = count + 1;
            }
            else
            {
                counts[item] = 1;
            }

            set.Add(item);

            // emit the minimum trakced item
            yield return set.Min;
        }
    }

    public static IEnumerable<decimal> MovingMax(this IEnumerable<decimal> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));

        var queue = new Queue<decimal>();
        var counts = new Dictionary<decimal, int>();
        var set = new SortedSet<decimal>();

        foreach (var item in source)
        {
            // remove the oldest item
            if (queue.Count >= periods)
            {
                var old = queue.Dequeue();
                if (--counts[old] == 0)
                {
                    set.Remove(old);
                }
            }

            // track the new item
            queue.Enqueue(item);

            if (counts.TryGetValue(item, out var count))
            {
                counts[item] = count + 1;
            }
            else
            {
                counts[item] = 1;
            }

            set.Add(item);

            // emit the minimum trakced item
            yield return set.Max;
        }
    }
}