#nullable enable
using System;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    /// <summary>
    /// Extension methods for shuffling, random sampling, and other list/span operations not
    /// covered by <see cref="System.Linq"/> (kept allocation-conscious rather than IEnumerable-based).
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Shuffles <paramref name="span"/> in place using the Fisher-Yates algorithm and
        /// <see cref="UnityEngine.Random"/>.
        /// </summary>
        public static void Shuffle<T>(this Span<T> span)
        {
            int count = span.Length;

            for( int i = 0; i < count; i++ )
            {
                int j = UnityEngine.Random.Range(i, count);
                (span[i], span[j]) = (span[j], span[i]);
            }
        }

        /// <summary>
        /// Shuffles <paramref name="list"/> in place using the Fisher-Yates algorithm and
        /// <see cref="UnityEngine.Random"/>. Generic over the concrete list type (rather than
        /// taking a plain <see cref="IList{T}"/>) so indexer access can be specialized instead of
        /// going through an interface call on every swap.
        /// </summary>
        public static void Shuffle<T, TList>(this TList list) where TList : IList<T>
        {
            int count = list.Count;

            for( int i = 0; i < count; i++ )
            {
                int j = UnityEngine.Random.Range(i, count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Partially shuffles <paramref name="span"/> in place using <see cref="UnityEngine.Random"/>,
        /// so that afterward <c>span[0..n)</c> holds <paramref name="n"/> elements drawn uniformly at
        /// random, without replacement, from the whole span (order among that first run is random
        /// too). Cheaper than a full <see cref="Shuffle{T}(Span{T})"/> when only a handful of
        /// elements need to be random - the remaining <c>span[n..)</c> elements end up in some
        /// leftover order but are not themselves a random shuffle.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is negative or greater than the span's length.</exception>
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

        /// <inheritdoc cref="PartialShuffle{T}(Span{T}, int)"/>
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

        /// <summary>
        /// Shuffles <paramref name="span"/> in place using the Fisher-Yates algorithm, drawing from
        /// the given <paramref name="rng"/> instead of <see cref="UnityEngine.Random"/> - use this
        /// overload when the shuffle needs to be deterministic/seeded or reproducible off the main thread.
        /// </summary>
        public static void Shuffle<T>(this Span<T> span, Random rng)
        {
            int count = span.Length;

            for( int i = 0; i < count; i++ )
            {
                int j = rng.Next(i, count);
                (span[i], span[j]) = (span[j], span[i]);
            }
        }

        /// <inheritdoc cref="Shuffle{T}(Span{T}, Random)"/>
        public static void Shuffle<T, TList>(this TList list, Random rng) where TList : IList<T>
        {
            int count = list.Count;

            for( int i = 0; i < count; i++ )
            {
                int j = rng.Next(i, count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Partial shuffle, as <see cref="PartialShuffle{T}(Span{T}, int)"/>, drawing from the given
        /// <paramref name="rng"/> instead of <see cref="UnityEngine.Random"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is negative or greater than the span's length.</exception>
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

        /// <inheritdoc cref="PartialShuffle{T}(Span{T}, int, Random)"/>
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

        /// <summary>
        /// Picks <paramref name="count"/> elements of <paramref name="source"/>, uniformly at
        /// random without replacement, and adds them to <paramref name="destination"/> in their
        /// original relative order (use <see cref="PickRandomUniqueShuffled{T}(IReadOnlyList{T}, ICollection{T}, int)"/>
        /// if the picked elements' order should be randomized too). Implemented as a single pass
        /// over <paramref name="source"/> (Knuth's "Algorithm S" selection sampling) rather than
        /// shuffling an index buffer, so it's O(1) extra space regardless of <c>source.Count</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative or greater than <c>source.Count</c>.</exception>
        public static void PickRandomUnique<T>( this IReadOnlyList<T> source, ICollection<T> destination, int count)
        {
            int sourceCount = source.Count;
            if (count < 0 || count > sourceCount)
                throw new ArgumentOutOfRangeException(nameof(count));

            int remaining = sourceCount;
            int needed = count;
            for (int i = 0; i < sourceCount && needed > 0; i++)
            {
                if (UnityEngine.Random.Range(0, remaining) < needed)
                {
                    destination.Add(source[i]);
                    needed--;
                }
                remaining--;
            }
        }

        /// <summary>
        /// Picks <paramref name="count"/> elements of <paramref name="source"/>, uniformly at
        /// random without replacement, and returns them as a new array in their original relative
        /// order. See <see cref="PickRandomUnique{T}(IReadOnlyList{T}, ICollection{T}, int)"/> for
        /// the underlying algorithm.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative or greater than <c>source.Count</c>.</exception>
        public static T[] PickRandomUnique<T>( this IReadOnlyList<T> source, int count)
        {
            int sourceCount = source.Count;
            if (count < 0 || count > sourceCount)
                throw new ArgumentOutOfRangeException(nameof(count));

            var destination = new T[count];
            int remaining = sourceCount;
            int needed = count;
            int writeIndex = 0;
            for (int i = 0; i < sourceCount && needed > 0; i++)
            {
                if (UnityEngine.Random.Range(0, remaining) < needed)
                {
                    destination[writeIndex++] = source[i];
                    needed--;
                }
                remaining--;
            }
            return destination;
        }

        /// <summary>
        /// As <see cref="PickRandomUnique{T}(IReadOnlyList{T}, ICollection{T}, int)"/>, but the
        /// picked elements are also shuffled before being added, so their order doesn't just
        /// mirror their relative order in <paramref name="source"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative or greater than <c>source.Count</c>.</exception>
        public static void PickRandomUniqueShuffled<T>(this IReadOnlyList<T> source, ICollection<T> destination, int count)
        {
            T[] picked = source.PickRandomUnique(count);
            picked.AsSpan().Shuffle();
            foreach (var item in picked)
                destination.Add(item);
        }

        /// <inheritdoc cref="PickRandomUniqueShuffled{T}(IReadOnlyList{T}, ICollection{T}, int)"/>
        public static T[] PickRandomUniqueShuffled<T>(this IReadOnlyList<T> source, int count)
        {
            T[] picked = source.PickRandomUnique(count);
            picked.AsSpan().Shuffle();
            return picked;
        }

        /// <summary>
        /// Removes the element at <paramref name="index"/> in O(1) by overwriting it with the list's
        /// last element and truncating, instead of shifting every following element down as
        /// <see cref="List{T}.RemoveAt"/> would. Does not preserve element order.
        /// </summary>
        public static void RemoveBySwap<T>(this List<T> list, int index)
        {
            int lastId = list.Count - 1;
            list[index] = list[lastId];
            list.RemoveAt(lastId);
        }

        /// <summary>
        /// Finds the first occurrence of <paramref name="item"/> (via <see cref="List{T}.IndexOf(T)"/>)
        /// and removes it using <see cref="RemoveBySwap{T}(List{T}, int)"/> (O(1), order not
        /// preserved). Returns <see langword="false"/> without modifying the list if not found.
        /// </summary>
        public static bool RemoveBySwap<T>(this List<T> list, T item)
        {
            int index = list.IndexOf(item);
            if(index < 0)
                return false;
            list.RemoveBySwap(index);
            return true;
        }

        /// <summary>
        /// Null-safe, order-sensitive equality check: <see langword="true"/> if both lists are the
        /// same reference or <see langword="null"/>, or if both are non-null with equal <see cref="List{T}.Count"/>
        /// and pairwise-equal elements (via <see cref="EqualityComparer{T}.Default"/>) in the same order.
        /// </summary>
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

        /// <summary>
        /// Appends every element of <paramref name="list"/> to <paramref name="target"/>, reserving
        /// capacity for the combined count up front.
        /// </summary>
        public static void AddList<T>(this List<T> target, IList<T> list)
        {
            int count = list.Count;
            target.EnsureCapacity(target.Count + count);
            for( int i = 0; i < count; ++i )
                target.Add(list[i]);
        }

        /// <summary>
        /// Appends every element of <paramref name="set"/> to <paramref name="target"/>, reserving
        /// capacity for the combined count up front.
        /// </summary>
        public static void AddSet<T>(this List<T> target, HashSet<T> set)
        {
            int count = set.Count;
            target.EnsureCapacity(target.Count + count);
            foreach (var obj in set)
                target.Add(obj);
        }

        /// <inheritdoc cref="AddSet{T}(List{T}, HashSet{T})"/>
        public static void AddSet<T>(this List<T> target, ReadonlySetView<T> set)
        {
            int count = set.Count;
            target.EnsureCapacity(target.Count + count);
            foreach (var obj in set)
                target.Add(obj);
        }

        /// <summary>
        /// Grows <paramref name="list"/>'s <see cref="List{T}.Capacity"/> to at least
        /// <paramref name="minCapacity"/> if it isn't already that large. Never shrinks capacity.
        /// </summary>
        public static void EnsureCapacity<T>(this List<T> list, int minCapacity)
        {
            list.Capacity = System.Math.Max(list.Capacity, minCapacity);
        }
    }
}