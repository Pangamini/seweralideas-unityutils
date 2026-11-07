#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace SeweralIdeas.Collections
{
    /// <summary>
    /// A copy of a collection taken from the pool, to loop over while the collection itself changes in the loop. Made by
    /// <see cref="SnapshotExtensions.Snapshot{T}(HashSet{T})"/> and the like, used directly in a <c>foreach</c>:
    /// <code>foreach(Avatar avatar in perch.PerchedAvatars.Snapshot()) avatar.ThrowOffPerch(speed);</code>
    /// The foreach gives the copy back to the pool when it ends, however it ends. It is for one loop: do not keep it, and do not
    /// enumerate it twice. Without a foreach nothing gives the copy back, which only costs the pool its reuse.
    /// </summary>
    public readonly struct Snapshot<T>
    {
        private readonly List<T> _items;

        internal Snapshot(List<T> items) => _items = items;

        public Enumerator GetEnumerator() => new(_items);

        public struct Enumerator : IDisposable
        {
            private readonly List<T> _items;
            private          int     _index;

            internal Enumerator(List<T> items)
            {
                _items = items;
                _index = -1;
            }

            public T Current => _items[_index];

            public bool MoveNext() => ++_index < _items.Count;

            public void Dispose() => ListPool<T>.Release(_items);
        }
    }

    public static class SnapshotExtensions
    {
        private static List<T> Rent<T>() => ListPool<T>.Get();

        public static Snapshot<T> Snapshot<T>(this HashSet<T> set)
        {
            List<T> items = Rent<T>();
            items.AddSet(set);
            return new Snapshot<T>(items);
        }

        public static Snapshot<T> Snapshot<T>(this ReadonlySetView<T> set)
        {
            List<T> items = Rent<T>();
            items.AddSet(set);
            return new Snapshot<T>(items);
        }

        public static Snapshot<T> Snapshot<T>(this ReadonlyObservableSet<T> set)
        {
            List<T> items = Rent<T>();
            items.AddSet(set);
            return new Snapshot<T>(items);
        }

        public static Snapshot<T> Snapshot<T>(this ReadonlyMultiSet<T> set)
        {
            List<T> items = Rent<T>();
            items.EnsureCapacity(set.Count);
            foreach(T obj in set)
                items.Add(obj);
            return new Snapshot<T>(items);
        }

        public static Snapshot<T> Snapshot<T>(this ObservableSet<T> set) => set.GetReadonly().Snapshot();

        public static Snapshot<T> Snapshot<T>(this List<T> list)
        {
            List<T> items = Rent<T>();
            items.AddRange(list);
            return new Snapshot<T>(items);
        }

        public static Snapshot<T> Snapshot<T>(this T[] array)
        {
            List<T> items = Rent<T>();
            items.AddRange(array);
            return new Snapshot<T>(items);
        }

        /// <summary>Any other list: a read-only view of one, for example. (Not enumerated, so nothing is allocated for that.)</summary>
        public static Snapshot<T> Snapshot<T>(this IReadOnlyList<T> list)
        {
            List<T> items = Rent<T>();
            int count = list.Count;
            items.EnsureCapacity(count);
            for(int i = 0; i < count; i++)
                items.Add(list[i]);
            return new Snapshot<T>(items);
        }
    }
}
