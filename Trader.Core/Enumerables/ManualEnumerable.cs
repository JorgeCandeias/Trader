using System.Collections;

namespace Outcompute.Trader.Core.Enumerables
{
    /// <summary>
    /// An enumerable whose values are supplied at runtime just before enumeration.
    /// </summary>
    public class ManualEnumerable<T> : IEnumerable<T>
    {
        private T value;



        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}