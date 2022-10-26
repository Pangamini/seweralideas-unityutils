using System.Collections.Generic;

namespace SeweralIdeas.Utils
{
    public static class Extensions
    {
        public static void AddList<T>(this List<T> target, IList<T> list)
        {
            int count = list.Count;
            target.EnsureCapacity(target.Count + count);
            for (int i = 0; i < count; ++i)
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