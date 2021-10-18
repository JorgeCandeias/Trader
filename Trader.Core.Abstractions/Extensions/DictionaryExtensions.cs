namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory)
        {
            if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            if (!dictionary.TryGetValue(key, out var value))
            {
                dictionary[key] = value = factory();
            }

            return value;
        }

        public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> addFactory, Func<TValue, TValue> updateFactory)
        {
            if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));
            if (addFactory is null) throw new ArgumentNullException(nameof(addFactory));
            if (updateFactory is null) throw new ArgumentNullException(nameof(updateFactory));

            if (dictionary.TryGetValue(key, out var value))
            {
                dictionary[key] = value = updateFactory(value);
            }
            else
            {
                dictionary[key] = value = addFactory();
            }

            return value;
        }

        public static TValue AddOrUpdate<TKey, TValue, TArg>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TArg, TValue> addFactory, Func<TArg, TValue, TValue> updateFactory, TArg arg)
        {
            if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));
            if (addFactory is null) throw new ArgumentNullException(nameof(addFactory));
            if (updateFactory is null) throw new ArgumentNullException(nameof(updateFactory));

            if (dictionary.TryGetValue(key, out var value))
            {
                dictionary[key] = value = updateFactory(arg, value);
            }
            else
            {
                dictionary[key] = value = addFactory(arg);
            }

            return value;
        }
    }
}