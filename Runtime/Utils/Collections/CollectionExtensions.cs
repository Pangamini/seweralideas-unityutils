using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public static class CollectionExtensions
    {
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
        
        public static bool ContentEquals<T>(List<T> lhs, List<T> rhs)
        {
            //first, compare for equality
            if (lhs == rhs)
                return true;

            // if one of lists is null, return false (since we already compared them for equality, both can't be null here)
            if (lhs == null || rhs == null)
                return false;

            // if count is different, return false
            if (lhs.Count != rhs.Count)
                return false;

            // compare elements
            for (int i = 0; i < lhs.Count; ++i)
            {
                if (!EqualityComparer<T>.Default.Equals(lhs[i], rhs[i]))
                    return false;
            }

            return true;
        }
    }
}