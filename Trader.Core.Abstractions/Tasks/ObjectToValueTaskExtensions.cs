using System.Threading.Tasks;

namespace System
{
    public static class ObjectToValueTaskExtensions
    {
        public static ValueTask<T> AsValueTaskResult<T>(this T obj)
        {
            return ValueTask.FromResult(obj);
        }
    }
}