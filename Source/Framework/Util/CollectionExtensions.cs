﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Runtime.CompilerServices;
using Google.Protobuf.WellKnownTypes;

namespace System.Collections.Generic
{
    public static class CollectionExtensions
    {
        public static bool Empty<T>(this Queue<T> queue)
        {
            return queue.Count == 0;
        }

        public static bool Empty<TValue>(this ICollection<TValue> collection)
        {
            return collection.Count == 0;
        }

        public static bool Empty<Tkey, TValue>(this IDictionary<Tkey, TValue> dictionary)
        {
            return dictionary.Count == 0;
        }

        /// <summary>
        ///  Returns the entry in this list at the given index, or the default value of the element
        ///  type if the index was out of bounds.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <param name="list">The list to retrieve from.</param>
        /// <param name="index">The index to try to retrieve at.</param>
        /// <returns>The value, or the default value of the element type.</returns>
        public static T LookupByIndex<T>(this IList<T> list, int index)
        {
            return index >= list.Count ? default : list[index];
        }

        /// <summary>
        ///  Returns the entry in this dictionary at the given key, or the default value of the key
        ///  if none.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="dict">The dictionary to operate on.</param>
        /// <param name="key">The key of the element to retrieve.</param>
        /// <returns>The value (if any).</returns>
        public static TValue LookupByKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, object key)
        {
            TValue val;
            TKey newkey = (TKey)Convert.ChangeType(key, typeof(TKey));

            return dict.TryGetValue(newkey, out val) ? val : default;
        }

        public static TValue LookupByKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            TValue val;

            return dict.TryGetValue(key, out val) ? val : default;
        }

        public static KeyValuePair<TKey, TValue> Find<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            if (!dict.ContainsKey(key))
                return default;

            return new KeyValuePair<TKey, TValue>(key, dict[key]);
        }

        public static bool ContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, object key)
        {
            TKey newkey = (TKey)Convert.ChangeType(key, typeof(TKey));

            return dict.ContainsKey(newkey);
        }

        public static void RemoveAll<T>(this List<T> collection, ICheck<T> check)
        {
            collection.RemoveAll(check.Invoke);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => Guid.NewGuid());
        }

        public static void Swap<T>(this T[] array, int position1, int position2)
        {
            //
            // Swaps elements in an array. Doesn't need to return a reference.
            //
            T temp = array[position1];           // Copy the first position's element
            array[position1] = array[position2]; // Assign to the second element
            array[position2] = temp;             // Assign to the first element
        }

        public static void Resize<T>(this List<T> list, uint size)
        {
            int cur = list.Count;

            if (size < cur)
                list.RemoveRange((int)size, cur - (int)size);
            else
                for (var i = list.Count; i < size; ++i)
                    list.Add(default);
        }

        public static void RandomResize<T>(this IList<T> list, uint size)
        {
            int listSize = list.Count;

            while (listSize > size)
            {
                list.RemoveAt(RandomHelper.IRand(0, listSize));
                --listSize;
            }
        }

        public static void RandomResize<T>(this List<T> list, Predicate<T> predicate, uint size)
        {
            for (var i = 0; i < list.Count; ++i)
            {
                var obj = list[i];

                if (!predicate(obj))
                    list.Remove(obj);
            }

            if (size != 0)
                list.Resize(size);
        }

        public static T SelectRandom<T>(this IEnumerable<T> source)
        {
            return source.SelectRandom(1).Single();
        }

        public static IEnumerable<T> SelectRandom<T>(this IEnumerable<T> source, uint count)
        {
            return source.Shuffle().Take((int)count);
        }

        public static T SelectRandomElementByWeight<T>(this IEnumerable<T> sequence, Func<T, float> weightSelector)
        {
            float totalWeight = sequence.Sum(weightSelector);
            // The weight we are after...
            float itemWeightIndex = (float)RandomHelper.NextDouble() * totalWeight;
            float currentWeightIndex = 0;

            foreach (var item in from weightedItem in sequence
                                 select new
                                 {
                                     Value = weightedItem,
                                     Weight = weightSelector(weightedItem)
                                 })
            {
                currentWeightIndex += item.Weight;

                // If we've hit or passed the weight we are after for this item then it's the one we want....
                if (currentWeightIndex >= itemWeightIndex)
                    return item.Value;
            }

            return default;
        }

        public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TSource, bool> comparer)
        {
            return first.Where(x => second.Count(y => comparer(x, y)) == 1);
        }

        public static uint[] ToBlockRange(this BitSet array)
        {
            uint[] blockValues = new uint[array.Length / 32 + 1];
            array.CopyTo(blockValues, 0);

            return blockValues;
        }

        public static void Clear(this Array array)
        {
            Array.Clear(array, 0, array.Length);
        }

        public static void EnsureWritableListIndex<T>(this List<T> list, uint index, T defaultValue)
        {
            while (list.Count <= index)
                list.Add(defaultValue);
        }

        public static void AddToDictList<T, L>(this Dictionary<T, List<L>> dict, T key, L item)
        {
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<L>();
                dict.Add(key, list);
            }

            list.Add(item);
        }

        public static void RemoveIf<T>(this LinkedList<T> values, Func<T, bool> func)
        {
            var toRemove = new List<T>();

            foreach (var v in values)
                if (func.Invoke(v))
                    toRemove.Add(v);

            foreach (var v in toRemove)
                values.Remove(v);
        }
        public static void RemoveIf<T>(this List<T> values, Func<T, bool> func)
        {
            var toRemove = new List<T>();

            foreach (var v in values)
                if (func.Invoke(v))
                    toRemove.Add(v);

            foreach (var v in toRemove)
                values.Remove(v);
        }

        public static void RemoveIf<T>(this List<T> values, ICheck<T> check)
        {
            RemoveIf(values, check.Invoke);
        }

        public static void RemoveIf<T>(this LinkedList<T> values, ICheck<T> check)
        {
            RemoveIf(values, check.Invoke);
        }

        public static bool has_value(this object obj)
        { 
            return obj != null; 
        }

        public static void Add<T, V>(this IDictionary<T, List<V>> dict, T key, V val)
        {
            if (dict == null) throw new ArgumentNullException();

            if (!dict.TryGetValue(key, out var list))
            {
                list= new List<V>();
                dict.Add(key, list);
            }
            list.Add(val);
        }

        public static void RemoveIf<TKey, TValue>(this IDictionary<TKey, TValue> dict, Func<TKey, bool> pred)
        {
            List<TKey> toRemove = new();

            foreach (var item in dict)
                if (pred(item.Key))
                    toRemove.Add(item.Key);

            dict.Remove(toRemove);
        }

        public static void RemoveIf<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, Func<TKey, TValue, bool> pred)
        {
            foreach (var item in dict)
            {
                List<TValue> toRemove = new();

                foreach (var val in item.Value)
                    if (pred(item.Key, val))
                        toRemove.Add(val);
                
                item.Value.Remove(toRemove);
            }
        }

        public static void Remove<T>(this List<T> list, List<T> toRemove)
        {
            foreach (var val in toRemove)
                list.Remove(val);
        }

        public static void Remove<TKey, TValue>(this IDictionary<TKey, TValue> dict, List<TKey> toRemove)
        {
            foreach (var val in toRemove)
                dict.Remove(val);
        }
    }

    public interface ICheck<in T>
    {
        bool Invoke(T obj);
    }

    public interface IDoWork<in T>
    {
        void Invoke(T obj);
    }
}