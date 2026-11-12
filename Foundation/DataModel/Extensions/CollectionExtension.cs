namespace SecondMonitor.DataModel.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public static class CollectionExtension
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (T x in collection)
            {
                action(x);
            }
        }

        public static int IndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            int index = 0;
            foreach (var item in source)
            {
                if (predicate.Invoke(item))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Func<TKey, bool> predicate)
        {
            IEnumerable<TKey> toRemove = dictionary.Keys.Where(predicate);
            toRemove.ForEach(x => dictionary.Remove(x));
        }

        public static IEnumerable<TResult> SelectWithPrevious<TSource, TResult>(this IEnumerable<TSource> source,
            Func<TSource, TSource, TResult> projection)
        {
            using (IEnumerator<TSource> iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext())
                {
                    yield break;
                }

                TSource previous = iterator.Current;
                while (iterator.MoveNext())
                {
                    yield return projection(previous, iterator.Current);
                    previous = iterator.Current;
                }
            }
        }

        public static IEnumerable<TSource> WhereWithPrevious<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, bool> checkFunction)
        {
            using (IEnumerator<TSource> iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext())
                {
                    yield break;
                }

                TSource previous = iterator.Current;
                yield return previous;

                while (iterator.MoveNext())
                {
                    if (!checkFunction(previous, iterator.Current))
                    {
                        continue;
                    }

                    previous = iterator.Current;
                    yield return iterator.Current;
                }
            }
        }

        public static void MatchLength<TSource>(this Collection<TSource> source, int length, Func<TSource> createNewItemAction)
        {
            if (source.Count == length)
            {
                return;
            }

            while (source.Count < length)
            {
                source.Add(createNewItemAction());
            }

            while (source.Count > length)
            {
                source.RemoveAt(0);
            }
        }
    }
}