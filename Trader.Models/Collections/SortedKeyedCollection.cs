using System.Collections.ObjectModel;

namespace Outcompute.Trader.Models.Collections
{
    public abstract class SortedKeyedCollection<TKey, TItem> : KeyedCollection<TKey, TItem>
        where TKey : notnull
    {
        protected virtual IComparer<TKey> KeyComparer => Comparer<TKey>.Default;

        protected override void InsertItem(int index, TItem item)
        {
            var insert = index;

            for (var i = 0; i < Count; i++)
            {
                TItem current = this[i];
                if (KeyComparer.Compare(GetKeyForItem(item), GetKeyForItem(current)) < 0)
                {
                    insert = i;
                    break;
                }
            }

            base.InsertItem(insert, item);
        }
    }
}