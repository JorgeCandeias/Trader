using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal class BandComparer : IComparer<Band>
    {
        private BandComparer()
        {
        }

        public static BandComparer Default { get; } = new BandComparer();

        public int Compare(Band? x, Band? y)
        {
            if (x is null)
            {
                if (y is null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (y is null)
                {
                    return 1;
                }
                else
                {
                    return CompareCore(x, y);
                }
            }
        }

        private static int CompareCore(Band x, Band y)
        {
            var byOpenPrice = Comparer<decimal>.Default.Compare(x.OpenPrice, y.OpenPrice);
            if (byOpenPrice is not 0)
            {
                return byOpenPrice;
            }

            var byOpenOrderId = Comparer<long>.Default.Compare(x.OpenOrderId, y.OpenOrderId);
            if (byOpenOrderId is not 0)
            {
                return byOpenOrderId;
            }

            var byId = Comparer<Guid>.Default.Compare(x.Id, y.Id);
            if (byId is not 0)
            {
                return byId;
            }

            return 0;
        }
    }
}