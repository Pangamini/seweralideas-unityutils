#nullable enable
using System;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public static class CollectionExtensions
    {
        public static void Shuffle<T>(this Span<T> span)
        {
            int count = span.Length;
            
            for( int i = 0; i < count; i++ )
            {
                int j = UnityEngine.Random.Range(i, count);
                (span[i], span[j]) = (span[j], span[i]);
            }
        }

        public static void Shuffle<T, TList>(this TList list) where TList : IList<T>
        {
            int count = list.Count;
            
            for( int i = 0; i < count; i++ )
            {
                int j = UnityEngine.Random.Range(i, count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        
        public static void PartialShuffle<T>(this Span<T> span, int n)
        {
            int count = span.Length;
            if (n < 0 || n > count)
                throw new ArgumentOutOfRangeException(nameof(n));

            for (int i = 0; i < n; i++)
            {
                int j = UnityEngine.Random.Range(i, count);
                (span[i], span[j]) = (span[j], span[i]);
            }
        }
        
        public static void PartialShuffle<T, TList>(this TList list, int n) where TList : IList<T>
        {
            int count = list.Count;
            if (n < 0 || n > count)
                throw new ArgumentOutOfRangeException(nameof(n));

            for (int i = 0; i < n; i++)
            {
                int j = UnityEngine.Random.Range(i, count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public static void Shuffle<T>(this Span<T> span, Random rng)
        {
            int count = span.Length;
            
            for( int i = 0; i < count; i++ )
            {
                int j = rng.Next(i, count);
                (span[i], span[j]) = (span[j], span[i]);
            }
        }

        public static void Shuffle<T, TList>(this TList list, Random rng) where TList : IList<T>
        {
            int count = list.Count;
            
            for( int i = 0; i < count; i++ )
            {
                int j = rng.Next(i, count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        
        public static void PartialShuffle<T>(this Span<T> span, int n, Random rng)
        {
            int count = span.Length;
            if (n < 0 || n > count)
                throw new ArgumentOutOfRangeException(nameof(n));

            for (int i = 0; i < n; i++)
            {
                int j = rng.Next(i, count);
                (span[i], span[j]) = (span[j], span[i]);
            }
        }
        
        public static void PartialShuffle<T, TList>(this TList list, int n, Random rng) where TList : IList<T>
        {
            int count = list.Count;
            if (n < 0 || n > count)
                throw new ArgumentOutOfRangeException(nameof(n));

            for (int i = 0; i < n; i++)
            {
                int j = rng.Next(i, count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        
        public static void PickRandomUnique<T>( this IReadOnlyList<T> source, ICollection<T> destination, int count)
        {
            int sourceCount = source.Count;
            if (count > sourceCount)
                throw new ArgumentOutOfRangeException(nameof(count));

            Span<int> indices = stackalloc int[sourceCount];

            for (int i = 0; i < sourceCount; i++)
                indices[i] = i;

            indices.PartialShuffle(count);

            for (int i = 0; i < count; i++)
                destination.Add(source[indices[i]]);
        }

        public static void RemoveBySwap<T>(this List<T> list, int index)
        {
            int lastId = list.Count - 1;
            list[index] = list[lastId];
            list.RemoveAt(lastId);
        }

        public static bool RemoveBySwap<T>(this List<T> list, T item)
        {
            int index = list.IndexOf(item);
            if(index < 0)
                return false;
            list.RemoveBySwap(index);
            return true;
        }

        public static bool ContentEquals<T>(List<T>? lhs, List<T>? rhs)
        {
            //first, compare for equality
            if(lhs == rhs)
                return true;

            // if one of lists is null, return false (since we already compared them for equality, both can't be null here)
            if(lhs == null || rhs == null)
                return false;

            // if count is different, return false
            if(lhs.Count != rhs.Count)
                return false;

            // compare elements
            for( int i = 0; i < lhs.Count; ++i )
            {
                if(!EqualityComparer<T>.Default.Equals(lhs[i], rhs[i]))
                    return false;
            }

            return true;
        }

        public static void AddList<T>(this List<T> target, IList<T> list)
        {
            int count = list.Count;
            target.EnsureCapacity(target.Count + count);
            for( int i = 0; i < count; ++i )
                target.Add(list[i]);
        }

        public static void AddSet<T>(this List<T> target, HashSet<T> set)
        {
            int count = set.Count;
            target.EnsureCapacity(target.Count + count);
            foreach (var obj in set)
                target.Add(obj);
        }

        public static void EnsureCapacity<T>(this List<T> list, int minCapacity)
        {
            list.Capacity = System.Math.Max(list.Capacity, minCapacity);
        }
    }
}